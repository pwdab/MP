using MP.Gameplay.Damage;
using MP.Gameplay.Entity;

namespace MP.Gameplay.Events
{
    /*
        EnemyKilled Event에 전달할 데이터 구조체
    */
    public readonly struct EnemyKilledEvent
    {
        public EnemyKilledEvent(EnemyEntity enemy, DamageContext damageContext)
        {
            Enemy = enemy;
            DamageContext = damageContext;
        }

        // 죽은 적 개체 정보
        // ※ 이벤트를 받는 시스템이 EnemyEntity라는 구체적 MonoBehaviour 타입을 알게 되므로 결합도가 조금 올라갈 수 있음
        // ※ 결합도를 낮추기 위해 EntityIdentity를 사용할 수 있음
        public EnemyEntity Enemy { get; }

        // 적을 죽인 마지막 피해의 발생 맥락
        public DamageContext DamageContext { get; }
    }
}
