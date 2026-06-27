using MP.Gameplay.Damage;
using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Gameplay.Stats;
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

            if (!characterState.CanAttack)
            {
                return;
            }

            if (!targeting.TryGetTarget(out Transform targetTransform, out HealthComponent targetHealth))
            {
                return;
            }

            float range = Mathf.Max(0f, stats.GetValue(StatId.AutoAttackRange));
            if (!IsTargetInAttackRange(targetTransform, range))
            {
                return;
            }

            int attackCount = attackScheduler.Tick(deltaTime, stats.GetValue(StatId.AttackSpeed));
            for (int i = 0; i < attackCount; i++)
            {
                DamageSystem.ApplyDamage(new DamageRequest(DamageContext.FromInstigator(gameObject), targetHealth, stats.GetValue(StatId.AttackPower)));
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
            if (targetCollider != null)
            {
                Vector2 closestPoint = targetCollider.ClosestPoint(transform.position);
                return ((Vector2)transform.position - closestPoint).sqrMagnitude <= range * range;
            }

            Vector2 origin = transform.position;
            Vector2 target = targetTransform.position;
            return (target - origin).sqrMagnitude <= range * range;
        }
    }
}
