using MP.Network;
using MP.Gameplay.Movement;
using MP.Gameplay.Stages;
using UnityEngine;

namespace MP.Gameplay.Combat
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
            if (!StageSimulationGate.CanRunCombatSimulation())
            {
                return;
            }

            if (combatants == null || combatants.Length == 0)
            {
                TickDynamicEnemyMovers(deltaTime);
                TickDynamicPlayerBasicAttackers(deltaTime);
                TickDynamicCombatants(deltaTime);
                TickDynamicProjectileAttackers(deltaTime);
                TickDynamicCastleAttackers(deltaTime);
                return;
            }

            TickDynamicEnemyMovers(deltaTime);
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
        }

        private static void TickDynamicEnemyMovers(float deltaTime)
        {
            EnemyMoveToCastleComponent[] movers = FindObjectsByType<EnemyMoveToCastleComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < movers.Length; i++)
            {
                movers[i].TickServer(deltaTime);
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

        private static void TickDynamicProjectileAttackers(float deltaTime)
        {
            AutoProjectileAttackComponent[] projectileAttackers = FindObjectsByType<AutoProjectileAttackComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < projectileAttackers.Length; i++)
            {
                projectileAttackers[i].TickServer(deltaTime);
            }
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
