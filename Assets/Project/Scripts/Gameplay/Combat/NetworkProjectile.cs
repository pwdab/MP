using MP.Gameplay.Damage;
using MP.Gameplay.Entity;
using MP.Network;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkProjectile : NetworkBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float radius = 0.18f;
        [SerializeField] private float lifetime = 2f;

        private Vector2 direction;
        private TeamId ownerTeam;
        private float damage;
        private float maxDistance;
        private float traveledDistance;
        private float remainingLifetime;
        private double serverSpawnTime;
        private bool initialized;
        private NetworkTransform networkTransform;

        private void Awake()
        {
            networkTransform = GetComponent<NetworkTransform>();
        }

        public override void OnNetworkSpawn()
        {
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
            direction = IsFinite(spawnDirection) && spawnDirection.sqrMagnitude > 0.0001f ? spawnDirection.normalized : Vector2.right;
            ownerTeam = team;
            damage = Mathf.Max(0f, projectileDamage);
            maxDistance = Mathf.Max(0f, projectileMaxDistance);
            traveledDistance = 0f;
            remainingLifetime = Mathf.Max(0f, lifetime);
            serverSpawnTime = GetServerTime();
            initialized = true;
        }

        public void PublishSpawnStateServer()
        {
            if (!NetworkContext.IsServer || !NetworkObject.IsSpawned)
            {
                return;
            }

            InitializeClientVisualClientRpc(direction, serverSpawnTime);
        }

        [ClientRpc]
        private void InitializeClientVisualClientRpc(Vector2 spawnDirection, double spawnTime)
        {
            if (NetworkContext.HasServerAuthority())
            {
                return;
            }

            direction = IsFinite(spawnDirection) && spawnDirection.sqrMagnitude > 0.0001f ? spawnDirection.normalized : Vector2.right;
            serverSpawnTime = spawnTime;

            float elapsedTime = GetElapsedTimeSinceServerSpawn();
            float moveDistance = Mathf.Max(0f, speed) * elapsedTime;
            transform.position += (Vector3)(direction * moveDistance);
            traveledDistance = moveDistance;
            remainingLifetime = Mathf.Max(0f, lifetime - elapsedTime);
            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (remainingLifetime <= 0f)
            {
                StopClientVisualOrDespawnServer();
                return;
            }

            if (NetworkContext.HasServerAuthority() && maxDistance <= 0f)
            {
                DespawnOrDestroyServerOnly();
                return;
            }

            float deltaTime = Time.deltaTime;
            float moveDistance = Mathf.Max(0f, speed) * deltaTime;
            transform.position += (Vector3)(direction * moveDistance);
            traveledDistance += moveDistance;
            remainingLifetime -= deltaTime;

            if (TryHitTarget())
            {
                DespawnOrDestroyServerOnly();
                return;
            }

            if (remainingLifetime <= 0f)
            {
                StopClientVisualOrDespawnServer();
                return;
            }

            if (NetworkContext.HasServerAuthority() && traveledDistance >= maxDistance)
            {
                DespawnOrDestroyServerOnly();
            }
        }

        public override void OnNetworkDespawn()
        {
            initialized = false;

            if (networkTransform != null)
            {
                networkTransform.enabled = true;
            }
        }

        private bool TryHitTarget()
        {
            if (!NetworkContext.HasServerAuthority())
            {
                return false;
            }

            var targets = TargetRegistry.ActiveTargets;
            float clampedRadius = Mathf.Max(0f, radius);
            float radiusSqr = clampedRadius * clampedRadius;
            Vector2 position = transform.position;

            for (int i = 0; i < targets.Count; i++)
            {
                TargetableComponent target = targets[i];
                if (target == null || !target.IsTargetable || !TeamUtility.AreEnemies(ownerTeam, target.Team))
                {
                    continue;
                }

                if ((target.Position - position).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                DamageSystem.ApplyDamage(new DamageRequest(gameObject, target.Health, damage));
                return true;
            }

            return false;
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

            initialized = false;
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

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
