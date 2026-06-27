using MP.Gameplay.Entity;
using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Gameplay.Movement
{
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(CharacterStateComponent))]
    [RequireComponent(typeof(EnemyTargetingComponent))]
    public sealed class EnemyEntityMovementComponent : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float contactDistance;

        private StatsComponent stats;
        private CharacterStateComponent characterState;
        private EnemyTargetingComponent targeting;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
            targeting = GetComponent<EnemyTargetingComponent>();
        }

        public void TickMovement(float deltaTime)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            if (!characterState.CanMove)
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
            float maxMoveDistance = Mathf.Max(0f, remainingDistance - contactDistance);
            float moveDistance = Mathf.Min(stats.GetValue(StatId.MoveSpeed) * deltaTime, maxMoveDistance);
            transform.position += (Vector3)(direction * moveDistance);
        }

        private float GetRemainingDistanceToAttackRange(Transform targetTransform, Vector2 toTarget)
        {
            float attackRange = Mathf.Max(0f, stats.GetValue(StatId.AutoAttackRange));
            Collider2D targetCollider = targetTransform.GetComponent<Collider2D>();
            if (targetCollider != null)
            {
                Vector2 closestPoint = targetCollider.ClosestPoint(transform.position);
                float distanceToTargetCollision = ((Vector2)transform.position - closestPoint).magnitude;
                return Mathf.Max(0f, distanceToTargetCollision - attackRange);
            }

            return Mathf.Max(0f, toTarget.magnitude - attackRange);
        }
    }
}

