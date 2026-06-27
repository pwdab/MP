namespace MP.Gameplay.Stages
{
    public interface IStageStateProvider
    {
        StageState CurrentStageState { get; }
    }
}
