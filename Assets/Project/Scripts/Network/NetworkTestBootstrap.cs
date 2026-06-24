using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
#endif

namespace MP.Network
{
    public sealed class NetworkTestBootstrap : MonoBehaviour
    {
        private const string LastHostPortEditorPrefKey = "MP.NetworkTestBootstrap.LastHostPort";
        private const string ReviveLocalPlayerMessageName = "MP.ReviveLocalPlayer";
        private const int HostPortSearchRange = 32;

        [SerializeField] private bool requireControlModifier = true;
        [SerializeField] private bool releaseHostPortBeforeStartInEditor = true;
        [SerializeField] private GameObject[] networkPrefabs;
        [SerializeField] private NetworkTestCommands testCommands;

        private bool registeredPrefabs;
        private bool registeredReviveMessageHandler;

        private void Update()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return;
            }

            if (WasStartHostPressedThisFrame() && !networkManager.IsListening)
            {
                PrepareHostPortBeforeStart(networkManager);
                RegisterNetworkPrefabs(networkManager);
                networkManager.StartHost();
            }

            if (WasStartClientPressedThisFrame() && !networkManager.IsListening)
            {
                ApplyLastHostPortBeforeClientStart(networkManager);
                RegisterNetworkPrefabs(networkManager);
                networkManager.StartClient();
            }

            if (WasShutdownPressedThisFrame() && networkManager.IsListening)
            {
                UnregisterReviveMessageHandler(networkManager);
                networkManager.Shutdown();
            }

            if (WasRestartPrototypePressedThisFrame())
            {
                RestartCurrentScene(networkManager);
                return;
            }

            if (WasRevivePlayerPressedThisFrame() && networkManager.IsListening && StageSimulationGate.CanAcceptPlayerInput())
            {
                RequestReviveLocalPlayer(networkManager);
            }

            UpdateReviveMessageHandler(networkManager);
        }

        private NetworkTestCommands GetTestCommands()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.LocalClient != null)
            {
                NetworkObject playerObject = networkManager.LocalClient.PlayerObject;
                if (playerObject != null && playerObject.TryGetComponent(out NetworkTestCommands localCommands))
                {
                    testCommands = localCommands;
                    return testCommands;
                }
            }

            if (testCommands == null)
            {
                testCommands = FindFirstObjectByType<NetworkTestCommands>();
            }

            return testCommands;
        }

        private void RequestReviveLocalPlayer(NetworkManager networkManager)
        {
            if (networkManager.IsServer)
            {
                RevivePlayerServer(networkManager.LocalClientId);
                return;
            }

            NetworkTestCommands commands = GetTestCommands();
            if (commands != null)
            {
                commands.RequestReviveLocalPlayer();
                return;
            }

            using var writer = new FastBufferWriter(1, Allocator.Temp);
            networkManager.CustomMessagingManager.SendNamedMessage(ReviveLocalPlayerMessageName, NetworkManager.ServerClientId, writer);
        }

        private void UpdateReviveMessageHandler(NetworkManager networkManager)
        {
            if (!networkManager.IsListening || !networkManager.IsServer)
            {
                UnregisterReviveMessageHandler(networkManager);
                return;
            }

            if (registeredReviveMessageHandler)
            {
                return;
            }

            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ReviveLocalPlayerMessageName, OnReviveLocalPlayerMessage);
            registeredReviveMessageHandler = true;
        }

        private void UnregisterReviveMessageHandler(NetworkManager networkManager)
        {
            if (!registeredReviveMessageHandler || networkManager.CustomMessagingManager == null)
            {
                return;
            }

            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ReviveLocalPlayerMessageName);
            registeredReviveMessageHandler = false;
        }

        private void OnReviveLocalPlayerMessage(ulong senderClientId, FastBufferReader _)
        {
            RevivePlayerServer(senderClientId);
        }

        private static void RevivePlayerServer(ulong ownerClientId)
        {
            if (!NetworkContext.HasServerAuthority())
            {
                return;
            }

            PlayerEntity[] players = FindObjectsByType<PlayerEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                PlayerEntity player = players[i];
                if (!player.TryGetComponent(out NetworkObject networkObject) || networkObject.OwnerClientId != ownerClientId)
                {
                    continue;
                }

                if (player.TryGetComponent(out RespawnComponent respawn))
                {
                    respawn.RespawnServer();
                }

                return;
            }
        }

        private void RegisterNetworkPrefabs(NetworkManager networkManager)
        {
            if (registeredPrefabs)
            {
                return;
            }

            if (networkPrefabs == null)
            {
                registeredPrefabs = true;
                return;
            }

            for (int i = 0; i < networkPrefabs.Length; i++)
            {
                GameObject prefab = networkPrefabs[i];
                if (prefab != null && !networkManager.NetworkConfig.Prefabs.Contains(prefab))
                {
                    networkManager.AddNetworkPrefab(prefab);
                }
            }

            registeredPrefabs = true;
        }

        private void PrepareHostPortBeforeStart(NetworkManager networkManager)
        {
#if UNITY_EDITOR_WIN
            ReleaseHostPortBeforeStart(networkManager);
            SelectAvailableHostPort(networkManager);
#elif UNITY_EDITOR
            StoreCurrentHostPort(networkManager);
#endif
        }

        private void ApplyLastHostPortBeforeClientStart(NetworkManager networkManager)
        {
#if UNITY_EDITOR
            if (!(networkManager.NetworkConfig.NetworkTransport is UnityTransport transport))
            {
                return;
            }

            int port = EditorPrefs.GetInt(LastHostPortEditorPrefKey, transport.ConnectionData.Port);
            if (port < 0 || port > ushort.MaxValue || port == transport.ConnectionData.Port)
            {
                return;
            }

            SetTransportPort(transport, (ushort)port);
            UnityEngine.Debug.Log($"Client transport port set to last Host port {port}.");
#endif
        }

        private void ReleaseHostPortBeforeStart(NetworkManager networkManager)
        {
#if UNITY_EDITOR_WIN
            if (!releaseHostPortBeforeStartInEditor)
            {
                return;
            }

            if (!(networkManager.NetworkConfig.NetworkTransport is UnityTransport transport))
            {
                return;
            }

            ushort port = transport.ConnectionData.Port;
            foreach (int processId in FindProcessIdsUsingUdpPort(port))
            {
                if (processId == Process.GetCurrentProcess().Id)
                {
                    continue;
                }

                TryKillProcess(processId, port);
            }
#endif
        }

        private static void StoreCurrentHostPort(NetworkManager networkManager)
        {
#if UNITY_EDITOR
            if (networkManager.NetworkConfig.NetworkTransport is UnityTransport transport)
            {
                EditorPrefs.SetInt(LastHostPortEditorPrefKey, transport.ConnectionData.Port);
            }
#endif
        }

#if UNITY_EDITOR
        private static void SetTransportPort(UnityTransport transport, ushort port)
        {
            UnityTransport.ConnectionAddressData connectionData = transport.ConnectionData;
            transport.SetConnectionData(connectionData.Address, port, connectionData.ServerListenAddress);
        }
#endif

#if UNITY_EDITOR_WIN
        private static void SelectAvailableHostPort(NetworkManager networkManager)
        {
            if (!(networkManager.NetworkConfig.NetworkTransport is UnityTransport transport))
            {
                return;
            }

            ushort requestedPort = transport.ConnectionData.Port;
            if (IsUdpPortAvailable(requestedPort))
            {
                StoreCurrentHostPort(networkManager);
                return;
            }

            if (!TryFindAvailableUdpPort(requestedPort, HostPortSearchRange, out ushort availablePort))
            {
                UnityEngine.Debug.LogWarning($"UDP port {requestedPort} is still in use and no available fallback port was found.");
                StoreCurrentHostPort(networkManager);
                return;
            }

            SetTransportPort(transport, availablePort);
            StoreCurrentHostPort(networkManager);
            UnityEngine.Debug.LogWarning($"UDP port {requestedPort} is still in use. Host transport port changed to {availablePort}.");
        }

        private static bool TryFindAvailableUdpPort(ushort startPort, int searchRange, out ushort availablePort)
        {
            for (int offset = 1; offset <= searchRange; offset++)
            {
                int candidate = startPort + offset;
                if (candidate > ushort.MaxValue)
                {
                    break;
                }

                if (IsUdpPortAvailable((ushort)candidate))
                {
                    availablePort = (ushort)candidate;
                    return true;
                }
            }

            availablePort = 0;
            return false;
        }

        private static bool IsUdpPortAvailable(ushort port)
        {
            try
            {
                using var udpClient = new UdpClient(port);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private static IEnumerable<int> FindProcessIdsUsingUdpPort(ushort port)
        {
            string output = RunProcess("netstat", "-ano -p UDP");
            if (string.IsNullOrWhiteSpace(output))
            {
                yield break;
            }

            string portSuffix = ":" + port;
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("UDP", StringComparison.OrdinalIgnoreCase) || !line.Contains(portSuffix))
                {
                    continue;
                }

                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3 || !int.TryParse(parts[parts.Length - 1], out int processId))
                {
                    continue;
                }

                yield return processId;
            }
        }

        private static void TryKillProcess(int processId, ushort port)
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                UnityEngine.Debug.LogWarning($"Port {port} is already in use by process '{process.ProcessName}' (PID {processId}). Killing it before starting Host.");
                process.Kill();
                process.WaitForExit(1000);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"Failed to kill process {processId} using port {port}: {exception.Message}");
            }
        }

        private static string RunProcess(string fileName, string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using Process process = Process.Start(startInfo);
                if (process == null)
                {
                    return string.Empty;
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(1000);
                return output;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"Failed to run '{fileName} {arguments}': {exception.Message}");
                return string.Empty;
            }
        }
#endif

        private bool WasStartHostPressedThisFrame()
        {
            return WasShortcutPressedThisFrame(KeyCode.H);
        }

        private bool WasStartClientPressedThisFrame()
        {
            return WasShortcutPressedThisFrame(KeyCode.C);
        }

        private bool WasShutdownPressedThisFrame()
        {
            return WasShortcutPressedThisFrame(KeyCode.S);
        }

        private bool WasRevivePlayerPressedThisFrame()
        {
            return WasKeyPressedThisFrame(KeyCode.R) && !IsControlPressed();
        }

        private bool WasRestartPrototypePressedThisFrame()
        {
            return WasShortcutPressedThisFrame(KeyCode.R);
        }

        private bool WasShortcutPressedThisFrame(KeyCode keyCode)
        {
            if (requireControlModifier && !IsControlPressed())
            {
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            return keyCode switch
            {
                KeyCode.H => Keyboard.current.hKey.wasPressedThisFrame,
                KeyCode.C => Keyboard.current.cKey.wasPressedThisFrame,
                KeyCode.S => Keyboard.current.sKey.wasPressedThisFrame,
                KeyCode.R => Keyboard.current.rKey.wasPressedThisFrame,
                _ => false,
            };
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(keyCode);
#else
            return false;
#endif
        }

        private static bool WasKeyPressedThisFrame(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            return keyCode switch
            {
                KeyCode.R => Keyboard.current.rKey.wasPressedThisFrame,
                _ => false,
            };
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(keyCode);
#else
            return false;
#endif
        }

        private void RestartCurrentScene(NetworkManager networkManager)
        {
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            registeredPrefabs = false;
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        private static bool IsControlPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#else
            return false;
#endif
        }
    }
}
