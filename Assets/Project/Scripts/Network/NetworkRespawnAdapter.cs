using MP.Gameplay.Stages;
using UnityEngine;

namespace MP.Network
{
    /*
        RespawnComponent를 서버 권한에서 진행시키는 네트워크 어댑터
    */
    [RequireComponent(typeof(RespawnComponent))]
    public sealed class NetworkRespawnAdapter : MonoBehaviour
    {
        private RespawnComponent respawn;

        private void Awake()
        {
            respawn = GetComponent<RespawnComponent>();
        }

        private void Update()
        {
            if (!NetworkContext.HasServerAuthority() || !StageSimulationGate.CanRunCombatSimulation())
            {
                return;
            }

            respawn.TickRespawn(Time.deltaTime);
        }

        public void RespawnServer()
        {
            if (!NetworkContext.HasServerAuthority() || !StageSimulationGate.CanRunCombatSimulation())
            {
                return;
            }

            respawn.Respawn();
        }
    }
}
