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
    public sealed class PlayerDirectionalBasicAttackComponent : MonoBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Player;
        [SerializeField, Range(1f, 180f)] private float attackHalfAngle = 45f;
        [SerializeField] private bool autoAttack = true;

        private readonly AttackScheduler attackScheduler = new();

        private StatsComponent stats;
        private CharacterStateComponent characterState;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
        }

        public void TickServer(float deltaTime)
        {
            if (!StageSimulationGate.CanRunCombatSimulation())
            {
                return;
            }

            if (!NetworkContext.HasServerAuthority() || !autoAttack || characterState == null || !characterState.CanAttack || !characterState.HasMoveDirection)
            {
                return;
            }

            EntityRuntimeStats runtimeStats = stats.Stats;
            int attackCount = attackScheduler.Tick(deltaTime, runtimeStats.AttackSpeed);
            for (int i = 0; i < attackCount; i++)
            {
                AttackInMoveDirection(runtimeStats);
            }
        }

        private void AttackInMoveDirection(EntityRuntimeStats runtimeStats)
        {
            Vector2 origin = transform.position;
            Vector2 direction = characterState.LastMoveDirection;
            float range = runtimeStats.AutoAttackRange;
            float rangeSqr = range * range;
            float minimumDot = Mathf.Cos(Mathf.Deg2Rad * attackHalfAngle);

            var targets = TargetRegistry.ActiveTargets;
            for (int i = 0; i < targets.Count; i++)
            {
                TargetableComponent target = targets[i];
                if (target == null || !target.IsTargetable || !TeamUtility.AreEnemies(team, target.Team))
                {
                    continue;
                }

                Vector2 toTarget = target.Position - origin;
                if (toTarget.sqrMagnitude > rangeSqr)
                {
                    continue;
                }

                if (toTarget.sqrMagnitude > 0.0001f && Vector2.Dot(direction, toTarget.normalized) < minimumDot)
                {
                    continue;
                }

                DamageSystem.ApplyDamage(new DamageRequest(DamageContext.FromInstigator(gameObject), target.Health, runtimeStats.AttackPower));
            }
        }
    }
}
