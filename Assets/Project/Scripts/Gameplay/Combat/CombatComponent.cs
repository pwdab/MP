using MP.Gameplay.Damage;
using MP.Gameplay.Entity;
using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    [RequireComponent(typeof(StatsComponent))]
    public sealed class CombatComponent : MonoBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Player;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private bool autoAttack = true;
        [SerializeField] private bool tickInUpdateForLocalTests;

        private readonly AttackScheduler attackScheduler = new();
        private readonly ITargetQuery targetQuery = new NaiveTargetQuery();

        private StatsComponent statsComponent;
        private HealthComponent health;

        private void Awake()
        {
            statsComponent = GetComponent<StatsComponent>();
            health = GetComponent<HealthComponent>();

            if (attackOrigin == null)
            {
                attackOrigin = transform;
            }
        }

        private void Update()
        {
            if (!tickInUpdateForLocalTests)
            {
                return;
            }

            TickServer(Time.deltaTime);
        }

        public void TickServer(float deltaTime)
        {
            if (!autoAttack || (health != null && health.IsDead))
            {
                return;
            }

            EntityRuntimeStats stats = statsComponent.Stats;
            int attackCount = attackScheduler.Tick(deltaTime, stats.AttackSpeed);

            for (int i = 0; i < attackCount; i++)
            {
                ExecuteAttack(stats);
            }
        }

        private void ExecuteAttack(EntityRuntimeStats stats)
        {
            Vector2 origin = attackOrigin.position;
            if (!targetQuery.TryFindNearestTarget(origin, stats.AutoAttackRange, team, out TargetableComponent target))
            {
                return;
            }

            var request = new DamageRequest(gameObject, target.Health, stats.AttackPower);
            DamageSystem.ApplyDamage(request);
        }
    }
}
