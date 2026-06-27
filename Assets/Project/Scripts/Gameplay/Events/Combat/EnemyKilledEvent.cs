using MP.Gameplay.Damage;
using UnityEngine;

namespace MP.Gameplay.Events
{
    /*
        EnemyKilled Event에 전달할 데이터 구조체
    */
    public readonly struct EnemyKilledEvent
    {
        public EnemyKilledEvent(string enemyId, Vector3 deathPosition, DamageContext damageContext)
        {
            EnemyId = enemyId ?? string.Empty;
            DeathPosition = deathPosition;
            DamageContext = damageContext;
        }

        // 퀘스트, 업적, 통계, 드랍 테이블 조회에 사용할 고정 식별자
        public string EnemyId { get; }

        public Vector3 DeathPosition { get; }

        // 마지막 피해의 발생 주체, 출처, 보상 수령자 등에 대한 맥락
        public DamageContext DamageContext { get; }
    }
}
