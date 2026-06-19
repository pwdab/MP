using MP.Gameplay.Entity;

namespace MP.Gameplay.Events
{
    // EnemyKilled Event에 전달할 데이터 구조체
    public readonly struct EnemyKilledEvent
    {
        public EnemyKilledEvent(EnemyEntity enemy)
        {
            Enemy = enemy;
        }

        // 죽인 적 개체 정보
        public EnemyEntity Enemy { get; }
    }
}
