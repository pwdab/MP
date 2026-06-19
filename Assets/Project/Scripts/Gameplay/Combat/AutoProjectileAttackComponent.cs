using MP.Gameplay.Entity;
using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(CharacterStateComponent))]
    public sealed class AutoProjectileAttackComponent : MonoBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Enemy;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private bool autoFire = true;

        private readonly AttackScheduler attackScheduler = new();
        private readonly ITargetQuery targetQuery = new NaiveTargetQuery();

        private StatsComponent statsComponent;
        private CharacterStateComponent characterState;

        private void Awake()
        {
            statsComponent = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();

            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        public void TickServer(float deltaTime)
        {
            if (!autoFire || projectilePrefab == null || characterState == null || !characterState.CanAttack)
            {
                return;
            }

            EntityRuntimeStats stats = statsComponent.Stats;
            int attackCount = attackScheduler.Tick(deltaTime, stats.AttackSpeed);

            for (int i = 0; i < attackCount; i++)
            {
                FireAtNearestTarget(stats);
            }
        }

        private void FireAtNearestTarget(EntityRuntimeStats stats)
        {
            Vector2 origin = firePoint.position;
            if (!targetQuery.TryFindNearestTarget(origin, stats.ProjectileRange, team, out TargetableComponent target))
            {
                return;
            }

            Vector2 direction = target.Position - origin;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.right;
            }
            else
            {
                direction.Normalize();
            }

            NetworkProjectileSpawner.TrySpawn(projectilePrefab, firePoint.position, direction, team, stats.AttackPower, stats.ProjectileRange);
        }
    }
}
