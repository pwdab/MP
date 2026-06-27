using MP.Gameplay.Events;
using MP.Gameplay.Stages;
using UnityEngine;

namespace MP.Network
{
    /*
        EnemyKilledEvent 구독과 서버 권한 판단을 담당하는 보상 시스템 어댑터
        실제 보상 계산과 지급은 Gameplay의 EnemyRewardSystem이 담당
    */
    [RequireComponent(typeof(EnemyRewardSystem))]
    public sealed class NetworkEnemyRewardSystemAdapter : MonoBehaviour
    {
        [SerializeField] private EnemyKilledEventChannel enemyKilledEventChannel;

        private EnemyRewardSystem rewardSystem;

        private void Awake()
        {
            rewardSystem = GetComponent<EnemyRewardSystem>();
        }

        private void OnEnable()
        {
            if (enemyKilledEventChannel != null)
            {
                enemyKilledEventChannel.Register(OnEnemyKilled);
            }
        }

        private void OnDisable()
        {
            if (enemyKilledEventChannel != null)
            {
                enemyKilledEventChannel.Unregister(OnEnemyKilled);
            }
        }

        private void OnEnemyKilled(EnemyKilledEvent eventData)
        {
            if (!NetworkContext.HasServerAuthority() || rewardSystem == null)
            {
                return;
            }

            rewardSystem.GrantReward(eventData);
        }
    }
}
