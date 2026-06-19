using MP.Gameplay.Entity;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace MP.UI
{
    public sealed class CombatNetworkTestHud : MonoBehaviour
    {
        private float smoothedDeltaTime;

        private void Update()
        {
            smoothedDeltaTime += (Time.unscaledDeltaTime - smoothedDeltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            const int width = 420;
            const int height = 205;

            GUILayout.BeginArea(new Rect(16, 16, width, height), GUI.skin.box);
            GUILayout.Label("Combat Network Test");
            GUILayout.Label($"View: {GetViewLabel()}");
            GUILayout.Label("Ctrl+H: Host    Ctrl+C: Client    Ctrl+S: Shutdown    Ctrl+R: Revive Player");
            GUILayout.Label("WASD: Move owned player    Left Click: Fire projectile");
            GUILayout.Label("Yellow ring: auto attack    Blue ring: projectile range");

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                GUILayout.Label("NetworkManager: missing");
            }
            else
            {
                GUILayout.Label($"Listening: {networkManager.IsListening}  Server: {networkManager.IsServer}  Client: {networkManager.IsClient}");
            }

            HealthComponent enemyHealth = FindEnemyHealth();
            if (enemyHealth != null)
            {
                GUILayout.Label($"Enemy HP: {enemyHealth.CurrentHealth:0}/{enemyHealth.MaxHealth:0}  Dead: {enemyHealth.IsDead}");
            }
            else
            {
                GUILayout.Label("Enemy HP: not found");
            }

            HealthComponent playerHealth = FindLocalPlayerHealth();
            if (playerHealth != null)
            {
                GUILayout.Label($"Player HP: {playerHealth.CurrentHealth:0}/{playerHealth.MaxHealth:0}  Dead: {playerHealth.IsDead}");
            }
            else
            {
                GUILayout.Label("Player HP: not found");
            }

            GUILayout.EndArea();

            DrawStatsPanel();
        }

        private void DrawStatsPanel()
        {
            const int width = 190;
            const int height = 78;
            Rect rect = new(Screen.width - width - 16, 16, width, height);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Runtime Stats");
            GUILayout.Label($"FPS: {GetFps():0}");
            GUILayout.Label($"RTT: {GetRttLabel()}");
            GUILayout.EndArea();
        }

        private float GetFps()
        {
            return smoothedDeltaTime > 0f ? 1f / smoothedDeltaTime : 0f;
        }

        private static string GetRttLabel()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
            {
                return "N/A";
            }

            if (networkManager.IsHost)
            {
                return "0 ms (host)";
            }

            if (networkManager.NetworkConfig.NetworkTransport is UnityTransport transport)
            {
                return $"{transport.GetCurrentRtt(networkManager.LocalClientId)} ms";
            }

            return "N/A";
        }

        private static string GetViewLabel()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return "No NetworkManager";
            }

            if (!networkManager.IsListening)
            {
                return "Offline / Not Started";
            }

            if (networkManager.IsHost)
            {
                return $"Host Player (LocalClientId: {networkManager.LocalClientId})";
            }

            if (networkManager.IsServer)
            {
                return "Server";
            }

            if (networkManager.IsClient)
            {
                return $"Client Player (LocalClientId: {networkManager.LocalClientId})";
            }

            return "Unknown";
        }

        private static HealthComponent FindEnemyHealth()
        {
            EnemyEntity enemy = FindFirstObjectByType<EnemyEntity>();
            if (enemy != null && enemy.TryGetComponent(out HealthComponent enemyHealth))
            {
                return enemyHealth;
            }

            HealthComponent[] healthComponents = FindObjectsByType<HealthComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < healthComponents.Length; i++)
            {
                HealthComponent health = healthComponents[i];
                if (health.GetComponent<EnemyEntity>() != null)
                {
                    return health;
                }
            }

            return null;
        }

        private static HealthComponent FindLocalPlayerHealth()
        {
            PlayerEntity[] players = FindObjectsByType<PlayerEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                PlayerEntity player = players[i];
                if (player == null || !player.TryGetComponent(out NetworkObject networkObject) || !networkObject.IsOwner)
                {
                    continue;
                }

                if (player.TryGetComponent(out HealthComponent health))
                {
                    return health;
                }
            }

            return null;
        }
    }
}
