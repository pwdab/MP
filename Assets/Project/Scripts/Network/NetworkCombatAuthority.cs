using MP.Gameplay.Combat;
using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkCombatAuthority : NetworkBehaviour
    {
        [SerializeField] private CombatSimulationRunner combatSimulationRunner;

        private void Awake()
        {
            EnsureCombatSimulationRunner();
        }

        public override void OnNetworkSpawn()
        {
            EnsureCombatSimulationRunner();
        }

        private void Update()
        {
            if (!IsServer || !EnsureCombatSimulationRunner())
            {
                return;
            }

            combatSimulationRunner.Tick(Time.deltaTime);
        }

        private bool EnsureCombatSimulationRunner()
        {
            if (combatSimulationRunner == null)
            {
                combatSimulationRunner = GetComponent<CombatSimulationRunner>();
            }

            if (combatSimulationRunner == null)
            {
                return false;
            }

            combatSimulationRunner.SetTickInUpdate(false);
            return true;
        }
    }
}
