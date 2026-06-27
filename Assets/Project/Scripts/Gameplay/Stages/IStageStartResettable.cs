namespace MP.Gameplay.Stages
{
    /*
        스테이지 시작 또는 부활 초기화 시 리셋되어야 하는 Gameplay/Network 컴포넌트용 인터페이스
        StageFlowController가 구체 컴포넌트 타입을 몰라도 초기화 요청을 보낼 수 있게 한다
    */
    public interface IStageStartResettable
    {
        void ResetForStageStart();
    }
}
