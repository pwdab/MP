using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Gameplay.Stats;
using System;
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
        [SerializeField] private bool useAutoAttackRange;
        [SerializeField] private float autoAttackRangeMultiplier = 1f;
        [SerializeField] private bool fireInMoveDirectionWhenNoTarget;

        private readonly AttackScheduler attackScheduler = new();
        private readonly ITargetQuery targetQuery = new NaiveTargetQuery();

        private StatsComponent statsComponent;
        private CharacterStateComponent characterState;

        public event Action<ProjectileSpawnRequest> ProjectileRequested;

        public float CurrentAttackRange
        {
            get
            {
                if (statsComponent == null)
                {
                    return 0f;
                }

                EntityRuntimeStats stats = statsComponent.Stats;
                return GetAttackRange(stats);
            }
        }

        private void Awake()
        {
            statsComponent = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();

            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        public void Tick(float deltaTime)
        {
            if (!StageSimulationGate.CanRunCombatSimulation())
            {
                return;
            }

            if (!autoFire || projectilePrefab == null || characterState == null || !characterState.CanAttack)
            {
                return;
            }

            EntityRuntimeStats stats = statsComponent.Stats;
            int attackCount = attackScheduler.Tick(deltaTime, stats.GetValue(StatId.AttackSpeed));

            for (int i = 0; i < attackCount; i++)
            {
                FireAtNearestTarget(stats);
            }
        }

        private void FireAtNearestTarget(EntityRuntimeStats stats)
        {
            Vector2 origin = firePoint.position;
            float attackRange = GetAttackRange(stats);
            Vector2 direction;
            if (targetQuery.TryFindNearestTarget(origin, attackRange, team, out TargetableComponent target))
            {
                direction = target.Position - origin;
            }
            else if (fireInMoveDirectionWhenNoTarget && characterState.HasMoveDirection)
            {
                direction = characterState.LastMoveDirection;
            }
            else
            {
                return;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.right;
            }
            else
            {
                direction.Normalize();
            }

            ProjectileRequested?.Invoke(new ProjectileSpawnRequest(projectilePrefab, firePoint.position, direction, team, stats.GetValue(StatId.AttackPower), attackRange, gameObject));
        }

        private float GetAttackRange(EntityRuntimeStats stats)
        {
            if (!useAutoAttackRange)
            {
                return stats.GetValue(StatId.AutoProjectileRange);
            }

            return stats.GetValue(StatId.AutoAttackRange) * Mathf.Max(0f, autoAttackRangeMultiplier);
        }
    }
}
