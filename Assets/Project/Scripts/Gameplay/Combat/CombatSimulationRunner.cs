using MP.Network;
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

            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (combatants == null || combatants.Length == 0)
            {
                TickDynamicCombatants(deltaTime);
                TickDynamicProjectileAttackers(deltaTime);
                return;
            }

            for (int i = 0; i < combatants.Length; i++)
            {
                CombatComponent combatant = combatants[i];
                if (combatant != null)
                {
                    combatant.TickServer(deltaTime);
                }
            }

            TickDynamicProjectileAttackers(deltaTime);
        }

        private static void TickDynamicCombatants(float deltaTime)
        {
            CombatComponent[] dynamicCombatants = FindObjectsByType<CombatComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < dynamicCombatants.Length; i++)
            {
                dynamicCombatants[i].TickServer(deltaTime);
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

        public void SetTickInUpdate(bool value)
        {
            tickInUpdate = value;
        }
    }
}
