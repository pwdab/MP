namespace MP.Gameplay.Stages
{
    /*
        외부 동기화 어댑터가 StageFlowController에 복제 상태를 주입하기 위한 인터페이스
        Gameplay 상태 머신은 네트워크 전송 방식을 알지 않는다
    */
    public interface IStageSnapshotReceiver
    {
        void ApplySnapshot(StageSnapshot snapshot);
    }
}
