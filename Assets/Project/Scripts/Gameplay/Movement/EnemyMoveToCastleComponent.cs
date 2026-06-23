using MP.Gameplay.Entity;
using MP.Gameplay.Stats;
using MP.Network;
using UnityEngine;

namespace MP.Gameplay.Movement
{
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(CharacterStateComponent))]
    [RequireComponent(typeof(EnemyTargetingComponent))]
    public sealed class EnemyMoveToCastleComponent : MonoBehaviour
    {
        [SerializeField] private float contactDistance = 0.01f;

        private StatsComponent stats;
        private CharacterStateComponent characterState;
        private EnemyTargetingComponent targeting;
        private Collider2D selfCollider;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
            targeting = GetComponent<EnemyTargetingComponent>();
            selfCollider = GetComponent<Collider2D>();
        }

        public void TickServer(float deltaTime)
        {
            if (!NetworkContext.HasServerAuthority() || !characterState.CanMove)
            {
                return;
            }

            if (!targeting.TryGetTarget(out Transform targetTransform, out _))
            {
                return;
            }

            Vector2 position = transform.position;
            Vector2 targetPosition = targetTransform.position;
            Vector2 toTarget = targetPosition - position;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float remainingDistance = GetRemainingDistanceToContact(targetTransform, toTarget);
            if (remainingDistance <= contactDistance)
            {
                return;
            }

            Vector2 direction = toTarget.normalized;
            float moveDistance = Mathf.Min(stats.MoveSpeed * deltaTime, remainingDistance);
            transform.position += (Vector3)(direction * moveDistance);
        }

        public void SetTargetCastle(CastleEntity castle)
        {
            targeting ??= GetComponent<EnemyTargetingComponent>();
            targeting.SetFallbackCastle(castle);
        }

        private float GetRemainingDistanceToContact(Transform targetTransform, Vector2 toTarget)
        {
            Collider2D targetCollider = targetTransform.GetComponent<Collider2D>();
            if (selfCollider != null && targetCollider != null)
            {
                ColliderDistance2D distance = selfCollider.Distance(targetCollider);
                return distance.isOverlapped ? 0f : Mathf.Max(0f, distance.distance);
            }

            float centerDistance = toTarget.magnitude;
            float combinedRadius = GetApproximateRadius(transform) + GetApproximateRadius(targetTransform);
            return Mathf.Max(0f, centerDistance - combinedRadius);
        }

        private static float GetApproximateRadius(Transform target)
        {
            if (target.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                Vector2 extents = spriteRenderer.bounds.extents;
                return Mathf.Max(extents.x, extents.y);
            }

            return 0f;
        }
    }
}
