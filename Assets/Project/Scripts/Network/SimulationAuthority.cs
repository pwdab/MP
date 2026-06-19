using UnityEngine;

namespace MP.Network
{
    public sealed class SimulationAuthority : MonoBehaviour
    {
        [SerializeField] private SimulationAuthorityMode mode = SimulationAuthorityMode.LocalOrServer;

        public SimulationAuthorityMode Mode => mode;

        public bool CanRunServerSimulation
        {
            get
            {
                if (mode == SimulationAuthorityMode.LocalOrServer)
                {
                    return NetworkContext.HasServerAuthority();
                }

                return NetworkContext.IsServer;
            }
        }
    }
}
