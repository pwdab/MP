using MP.Gameplay.Combat;
using MP.Gameplay.Movement;
using MP.Gameplay.Stages;
using UnityEngine;

namespace MP.Network
{
    public sealed class CombatSimulationRunner : MonoBehaviour
    {
        [SerializeField] private SimulationAuthority authority;
        [SerializeField] private CombatComponent[] combatants;
        [SerializeField] private bool tickInUpdate = true;

        private void Awake()
        {
            if (authority == null)
            {
                authority = FindFirstObjectByType<SimulationAuthority>();
            }
        }

        private void Update()
        {
            if (!tickInUpdate)
            {
                return;
            }

            if (authority != null && !authority.CanRunServerSimulation)
            {
                return;
            }

            if (!StageSimulationGate.CanRunCombatSimulation())
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            if (authority != null && !authority.CanRunServerSimulation)
            {
                return;
            }

            if (!StageSimulationGate.CanRunCombatSimulation())
            {
                return;
            }

            if (combatants == null || combatants.Length == 0)
            {
                EnsureDynamicRewardAdapters();
                TickDynamicEnemyMovers(deltaTime);
                TickDynamicPlayerKnockbacks(deltaTime);
                TickDynamicPlayerContactDamage();
                TickDynamicRespawns(deltaTime);
                TickDynamicPlayerBasicAttackers(deltaTime);
                TickDynamicCombatants(deltaTime);
                TickDynamicProjectileAttackers(deltaTime);
                TickDynamicCastleAttackers(deltaTime);
                TickDynamicPlayerSeparation();
                return;
            }

            EnsureDynamicRewardAdapters();
            TickDynamicEnemyMovers(deltaTime);
            TickDynamicPlayerKnockbacks(deltaTime);
            TickDynamicPlayerContactDamage();
            TickDynamicRespawns(deltaTime);
            TickDynamicPlayerBasicAttackers(deltaTime);

            for (int i = 0; i < combatants.Length; i++)
            {
                CombatComponent combatant = combatants[i];
                if (combatant != null)
                {
                    combatant.TickServer(deltaTime);
                }
            }

            TickDynamicProjectileAttackers(deltaTime);
            TickDynamicCastleAttackers(deltaTime);
            TickDynamicPlayerSeparation();
        }

        private static void TickDynamicEnemyMovers(float deltaTime)
        {
            EnemyEntityMovementComponent[] movers = FindObjectsByType<EnemyEntityMovementComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < movers.Length; i++)
            {
                movers[i].TickMovement(deltaTime);
            }
        }

        private static void TickDynamicCombatants(float deltaTime)
        {
            CombatComponent[] dynamicCombatants = FindObjectsByType<CombatComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < dynamicCombatants.Length; i++)
            {
                dynamicCombatants[i].TickServer(deltaTime);
            }
        }

        private static void TickDynamicPlayerBasicAttackers(float deltaTime)
        {
            PlayerDirectionalBasicAttackComponent[] attackers = FindObjectsByType<PlayerDirectionalBasicAttackComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < attackers.Length; i++)
            {
                attackers[i].TickServer(deltaTime);
            }
        }

        private static void TickDynamicPlayerKnockbacks(float deltaTime)
        {
            PlayerKnockbackMovementComponent[] knockbacks = FindObjectsByType<PlayerKnockbackMovementComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < knockbacks.Length; i++)
            {
                knockbacks[i].TickMovement(deltaTime);
            }
        }

        private static void TickDynamicPlayerContactDamage()
        {
            PlayerEnemyContactDamageComponent[] contactDamageComponents = FindObjectsByType<PlayerEnemyContactDamageComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < contactDamageComponents.Length; i++)
            {
                contactDamageComponents[i].TickContactDamage();
            }
        }

        private static void TickDynamicPlayerSeparation()
        {
            PlayerCollisionSeparationComponent[] separationComponents = FindObjectsByType<PlayerCollisionSeparationComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < separationComponents.Length; i++)
            {
                separationComponents[i].TickSeparation();
            }
        }

        private static void TickDynamicRespawns(float deltaTime)
        {
            RespawnComponent[] respawns = FindObjectsByType<RespawnComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < respawns.Length; i++)
            {
                respawns[i].TickRespawn(deltaTime);
            }
        }

        private static void EnsureDynamicRewardAdapters()
        {
            EnemyGoldDropComponent[] goldDrops = FindObjectsByType<EnemyGoldDropComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < goldDrops.Length; i++)
            {
                if (!goldDrops[i].TryGetComponent(out NetworkEnemyGoldDropAdapter _))
                {
                    goldDrops[i].gameObject.AddComponent<NetworkEnemyGoldDropAdapter>();
                }
            }
        }

        private static void TickDynamicProjectileAttackers(float deltaTime)
        {
            AutoProjectileAttackComponent[] projectileAttackers = FindObjectsByType<AutoProjectileAttackComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < projectileAttackers.Length; i++)
            {
                EnsureNetworkAutoProjectileAdapter(projectileAttackers[i]);
                projectileAttackers[i].Tick(deltaTime);
            }
        }

        private static void EnsureNetworkAutoProjectileAdapter(AutoProjectileAttackComponent projectileAttack)
        {
            if (projectileAttack == null || projectileAttack.TryGetComponent(out NetworkAutoProjectileAttackAdapter _))
            {
                return;
            }

            projectileAttack.gameObject.AddComponent<NetworkAutoProjectileAttackAdapter>();
        }

        private static void TickDynamicCastleAttackers(float deltaTime)
        {
            EnemyCastleAttackComponent[] castleAttackers = FindObjectsByType<EnemyCastleAttackComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < castleAttackers.Length; i++)
            {
                castleAttackers[i].TickServer(deltaTime);
            }
        }

        public void SetTickInUpdate(bool value)
        {
            tickInUpdate = value;
        }
    }
}
