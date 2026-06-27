namespace MP.Gameplay.Stats
{
    /*
        스탯 Id 정의
        새 스탯을 추가하면 StatCatalogDefinition 에셋과 각 EntityStatsDefinition 에셋의 값도 함께 확인해야 함
    */
    public enum StatId
    {
        MaxHealth,
        AttackPower,
        AttackSpeed,
        AutoAttackRange,
        AutoProjectileRange,
        ManualProjectileRange,
        MoveSpeed,
        RespawnDelay,
        Defense
    }
}
