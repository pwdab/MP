using MP.Gameplay.Combat;
using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace MP.Network
{
    /*
        투사체 네트워크 어댑터
        ProjectileComponent의 Gameplay Tick을 서버/클라이언트 상황에 맞게 실행하고, spawn/despawn 동기화만 담당
    */
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(ProjectileComponent))]
    public sealed class NetworkProjectile : NetworkBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float radius = 0.18f;
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private float knockbackDistance = 1f;
        [SerializeField] private float knockbackDuration = 0.5f;

        private double serverSpawnTime;
        private bool initialized;
        private NetworkTransform networkTransform;
        private SpriteRenderer spriteRenderer;
        private ProjectileComponent projectile;

        private void Awake()
        {
            networkTransform = GetComponent<NetworkTransform>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (!TryGetComponent(out projectile))
            {
                projectile = gameObject.AddComponent<ProjectileComponent>();
            }

            projectile.Configure(speed, radius, lifetime, knockbackDistance, knockbackDuration);
        }

        public override void OnNetworkSpawn()
        {
            SetVisualVisible(true);

            if (NetworkContext.HasServerAuthority())
            {
                return;
            }

            if (networkTransform != null)
            {
                networkTransform.enabled = false;
            }
        }

        public void InitializeServer(Vector2 spawnDirection, TeamId team, float projectileDamage, float projectileMaxDistance)
        {
            InitializeServer(spawnDirection, team, projectileDamage, projectileMaxDistance, null);
        }

        public void InitializeServer(Vector2 spawnDirection, TeamId team, float projectileDamage, float projectileMaxDistance, GameObject projectileInstigator)
        {
            projectile.Initialize(spawnDirection, team, projectileDamage, projectileMaxDistance, projectileInstigator);
            serverSpawnTime = GetServerTime();
            initialized = true;
        }

        public void PublishSpawnStateServer()
        {
            if (!NetworkContext.IsServer || !NetworkObject.IsSpawned)
            {
                return;
            }

            InitializeClientVisualClientRpc(projectile.SpawnPosition, projectile.Direction, serverSpawnTime, projectile.MaxDistance);
        }

        [ClientRpc]
        private void InitializeClientVisualClientRpc(Vector2 serverSpawnPosition, Vector2 spawnDirection, double spawnTime, float projectileMaxDistance)
        {
            if (NetworkContext.HasServerAuthority())
            {
                return;
            }

            serverSpawnTime = spawnTime;
            projectile.InitializeVisual(serverSpawnPosition, spawnDirection, projectileMaxDistance, GetElapsedTimeSinceServerSpawn());
            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (!StageSimulationGate.CanRunCombatSimulation())
            {
                StopClientVisualOrDespawnServer();
                return;
            }

            ProjectileTickResult result = projectile.Tick(Time.deltaTime, NetworkContext.HasServerAuthority());
            if (result == ProjectileTickResult.Running)
            {
                return;
            }

            if (NetworkContext.HasServerAuthority())
            {
                DespawnOrDestroyServerOnly();
                return;
            }

            StopClientVisual();
        }

        public override void OnNetworkDespawn()
        {
            initialized = false;

            if (networkTransform != null)
            {
                networkTransform.enabled = true;
            }

            SetVisualVisible(true);
        }

        private void DespawnOrDestroyServerOnly()
        {
            if (!NetworkContext.HasServerAuthority())
            {
                return;
            }

            NetworkObject networkObject = NetworkObject;
            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void StopClientVisualOrDespawnServer()
        {
            if (NetworkContext.HasServerAuthority())
            {
                DespawnOrDestroyServerOnly();
                return;
            }

            StopClientVisual();
        }

        private void StopClientVisual()
        {
            initialized = false;
            SetVisualVisible(false);
        }

        private void SetVisualVisible(bool visible)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }

        private float GetElapsedTimeSinceServerSpawn()
        {
            double elapsedTime = GetServerTime() - serverSpawnTime;
            if (double.IsNaN(elapsedTime) || double.IsInfinity(elapsedTime))
            {
                return 0f;
            }

            return Mathf.Max(0f, (float)elapsedTime);
        }

        private static double GetServerTime()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsListening ? networkManager.ServerTime.Time : 0d;
        }
    }
}
