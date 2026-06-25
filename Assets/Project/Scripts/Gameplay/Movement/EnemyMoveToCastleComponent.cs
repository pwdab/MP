using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
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
        [SerializeField] private float contactDistance;

        private StatsComponent stats;
        private CharacterStateComponent characterState;
        private EnemyTargetingComponent targeting;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
            targeting = GetComponent<EnemyTargetingComponent>();
        }

        public void TickServer(float deltaTime)
        {
            if (!StageSimulationGate.CanRunCombatSimulation())
            {
                return;
            }

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

            float remainingDistance = GetRemainingDistanceToAttackRange(targetTransform, toTarget);
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

        private float GetRemainingDistanceToAttackRange(Transform targetTransform, Vector2 toTarget)
        {
            float attackRange = Mathf.Max(0f, stats.AutoAttackRange);
            Collider2D targetCollider = targetTransform.GetComponent<Collider2D>();
            if (targetCollider != null)
            {
                Vector2 closestPoint = targetCollider.ClosestPoint(transform.position);
                float distanceToTargetCollision = ((Vector2)transform.position - closestPoint).magnitude;
                return Mathf.Max(0f, distanceToTargetCollision - attackRange);
            }

            float centerDistance = toTarget.magnitude;
            float targetRadius = GetApproximateRadius(targetTransform);
            return Mathf.Max(0f, centerDistance - targetRadius - attackRange);
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
