namespace MP.Gameplay.Stages
{
    public static class StageSimulationGate
    {
        public static bool CanRunCombatSimulation()
        {
            StageFlowController stageFlow = UnityEngine.Object.FindFirstObjectByType<StageFlowController>();
            return stageFlow == null || stageFlow.CurrentStageState == StageState.Playing;
        }

        public static bool CanAcceptPlayerInput()
        {
            StageFlowController stageFlow = UnityEngine.Object.FindFirstObjectByType<StageFlowController>();
            return stageFlow == null || (stageFlow.CurrentStageState != StageState.Failed && stageFlow.CurrentStageState != StageState.Cleared);
        }
    }
}
