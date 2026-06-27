namespace MP.Gameplay.Combat
{
    /*
        투사체 Tick 결과
        Network 어댑터나 다른 실행자가 투사체 수명 종료 처리를 결정할 때 사용
    */
    public enum ProjectileTickResult
    {
        Running,
        HitTarget,
        Expired,
        ReachedMaxDistance
    }
}
