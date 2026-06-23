using UnityEngine;

namespace MP.Gameplay.Entity
{
    public sealed class EnemyTargetingComponent : MonoBehaviour
    {
        [SerializeField] private CastleEntity fallbackCastle;
        [SerializeField] private float playerDetectionRange = 5f;

        public float PlayerDetectionRange => playerDetectionRange;

        public void SetFallbackCastle(CastleEntity castle)
        {
            fallbackCastle = castle;
        }

        public bool TryGetTarget(out Transform targetTransform, out HealthComponent targetHealth)
        {
            if (TryGetNearestDetectedPlayer(out PlayerEntity player))
            {
                targetTransform = player.transform;
                targetHealth = player.Health;
                return true;
            }

            CastleEntity castle = GetFallbackCastle();
            if (castle != null && !castle.IsDestroyed && castle.Health != null)
            {
                targetTransform = castle.transform;
                targetHealth = castle.Health;
                return true;
            }

            targetTransform = null;
            targetHealth = null;
            return false;
        }

        private bool TryGetNearestDetectedPlayer(out PlayerEntity nearestPlayer)
        {
            PlayerEntity[] players = FindObjectsByType<PlayerEntity>(FindObjectsSortMode.None);
            float range = Mathf.Max(0f, playerDetectionRange);
            float rangeSqr = range * range;
            float nearestDistanceSqr = float.MaxValue;
            nearestPlayer = null;
            Vector2 origin = transform.position;

            for (int i = 0; i < players.Length; i++)
            {
                PlayerEntity player = players[i];
                if (player == null || player.Health == null || player.Health.IsDead)
                {
                    continue;
                }

                float distanceSqr = ((Vector2)player.transform.position - origin).sqrMagnitude;
                if (distanceSqr > rangeSqr || distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                nearestPlayer = player;
            }

            return nearestPlayer != null;
        }

        private CastleEntity GetFallbackCastle()
        {
            if (fallbackCastle == null)
            {
                fallbackCastle = FindFirstObjectByType<CastleEntity>();
            }

            return fallbackCastle;
        }
    }
}
