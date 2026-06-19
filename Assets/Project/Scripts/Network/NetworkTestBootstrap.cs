using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MP.Network
{
    public sealed class NetworkTestBootstrap : MonoBehaviour
    {
        [SerializeField] private bool requireControlModifier = true;
        [SerializeField] private GameObject[] networkPrefabs;
        [SerializeField] private NetworkTestCommands testCommands;

        private bool registeredPrefabs;

        private void Update()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return;
            }

            if (WasStartHostPressedThisFrame() && !networkManager.IsListening)
            {
                RegisterNetworkPrefabs(networkManager);
                networkManager.StartHost();
            }

            if (WasStartClientPressedThisFrame() && !networkManager.IsListening)
            {
                RegisterNetworkPrefabs(networkManager);
                networkManager.StartClient();
            }

            if (WasShutdownPressedThisFrame() && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            if (WasRevivePlayerPressedThisFrame() && networkManager.IsListening)
            {
                GetTestCommands()?.RequestReviveLocalPlayer();
            }
        }

        private NetworkTestCommands GetTestCommands()
        {
            if (testCommands == null)
            {
                testCommands = FindFirstObjectByType<NetworkTestCommands>();
            }

            return testCommands;
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
