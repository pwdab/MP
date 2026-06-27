using MP.Gameplay.Entity;
using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    public static class NetworkProjectileSpawner
    {
        public static bool TrySpawn(
            GameObject projectilePrefab,
            Vector3 position,
            Vector2 direction,
            TeamId team,
            float damage,
            float maxDistance)
        {
            return TrySpawn(projectilePrefab, position, direction, team, damage, maxDistance, null);
        }

        public static bool TrySpawn(
            GameObject projectilePrefab,
            Vector3 position,
            Vector2 direction,
            TeamId team,
            float damage,
            float maxDistance,
            GameObject instigator)
        {
            if (!NetworkContext.HasServerAuthority() || projectilePrefab == null || !IsFinite(position) || !IsFinite(direction))
            {
                return false;
            }

            GameObject projectileObject = Object.Instantiate(projectilePrefab, position, Quaternion.identity);
            if (!projectileObject.TryGetComponent(out NetworkProjectile projectile) || !projectileObject.TryGetComponent(out NetworkObject _))
            {
                Object.Destroy(projectileObject);
                return false;
            }

            projectile.InitializeServer(direction, team, damage, maxDistance, instigator);
            if (NetworkSpawnUtility.TrySpawnNetworkObject(projectileObject))
            {
                projectile.PublishSpawnStateServer();
                return true;
            }

            Object.Destroy(projectileObject);
            return false;
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

