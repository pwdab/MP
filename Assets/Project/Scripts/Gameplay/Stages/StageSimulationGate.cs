namespace MP.Gameplay.Stages
{
    public static class StageSimulationGate
    {
        private static IStageStateProvider stageStateProvider;

        public static void Register(IStageStateProvider provider)
        {
            stageStateProvider = provider;
        }

        public static void Unregister(IStageStateProvider provider)
        {
            if (ReferenceEquals(stageStateProvider, provider))
            {
                stageStateProvider = null;
            }
        }

        public static bool CanRunCombatSimulation()
        {
            IStageStateProvider provider = GetStageStateProvider();
            return provider == null || provider.CurrentStageState == StageState.Playing;
        }

        public static bool CanAcceptPlayerInput()
        {
            IStageStateProvider provider = GetStageStateProvider();
            return provider == null || (provider.CurrentStageState != StageState.Failed && provider.CurrentStageState != StageState.Cleared);
        }

        private static IStageStateProvider GetStageStateProvider()
        {
            if (stageStateProvider != null)
            {
                return stageStateProvider;
            }

            UnityEngine.MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>(UnityEngine.FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IStageStateProvider provider)
                {
                    stageStateProvider = provider;
                    return stageStateProvider;
                }
            }

            return null;
        }
    }
}
