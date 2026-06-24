using MP.Gameplay.Damage;
using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Gameplay.Stats;
using MP.Network;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(CharacterStateComponent))]
    [RequireComponent(typeof(EnemyTargetingComponent))]
    public sealed class EnemyCastleAttackComponent : MonoBehaviour
    {
        private readonly AttackScheduler attackScheduler = new();

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
            if (!StageSimulationGate.CanRunCombatSimulation())
            {
                return;
            }

            if (!NetworkContext.HasServerAuthority() || !characterState.CanAttack)
            {
                return;
            }

            if (!targeting.TryGetTarget(out Transform targetTransform, out HealthComponent targetHealth))
            {
                return;
            }

            float range = Mathf.Max(0f, stats.AutoAttackRange);
            if (!IsTargetInAttackRange(targetTransform, range))
            {
                return;
            }

            int attackCount = attackScheduler.Tick(deltaTime, stats.AttackSpeed);
            for (int i = 0; i < attackCount; i++)
            {
                DamageSystem.ApplyDamage(new DamageRequest(gameObject, targetHealth, stats.AttackPower));
            }
        }

        public void SetTargetCastle(CastleEntity castle)
        {
            targeting ??= GetComponent<EnemyTargetingComponent>();
            targeting.SetFallbackCastle(castle);
        }

        private bool IsTargetInAttackRange(Transform targetTransform, float range)
        {
            Collider2D targetCollider = targetTransform.GetComponent<Collider2D>();
            if (selfCollider != null && targetCollider != null)
            {
                ColliderDistance2D distance = selfCollider.Distance(targetCollider);
                return distance.isOverlapped || distance.distance <= range;
            }

            Vector2 origin = transform.position;
            Vector2 target = targetTransform.position;
            return (target - origin).sqrMagnitude <= range * range;
        }
    }
}
