using MP.Gameplay.Entity;
using MP.Gameplay.Events;
using MP.Network;
using MP.Progression.Level;
using MP.UI;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    [RequireComponent(typeof(EnemyEntity))]
    public sealed class EnemyExperienceRewardComponent : MonoBehaviour
    {
        [SerializeField, Min(0)] private int experienceAmount = 1;
        [SerializeField] private EnemyKilledEventChannel enemyKilledEventChannel;

        private EnemyEntity enemy;

        private void Awake()
        {
            enemy = GetComponent<EnemyEntity>();
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
            if (eventData.Enemy != enemy || !NetworkContext.HasServerAuthority() || experienceAmount <= 0)
            {
                return;
            }

            if (!TryGetRewardReceiverProgression(eventData.DamageContext.RewardReceiver, out PlayerProgressionComponent progression))
            {
                return;
            }

            progression.AddExperience(experienceAmount);
            FloatingWorldText.Show(transform.position + Vector3.up, $"+{experienceAmount} EXP", new Color(0.35f, 0.8f, 1f, 1f));
        }

        private static bool TryGetRewardReceiverProgression(GameObject rewardReceiver, out PlayerProgressionComponent progression)
        {
            progression = null;
            if (rewardReceiver == null)
            {
                return false;
            }

            if (rewardReceiver.TryGetComponent(out progression))
            {
                return true;
            }

            progression = rewardReceiver.GetComponentInParent<PlayerProgressionComponent>();
            if (progression != null)
            {
                return true;
            }

            progression = rewardReceiver.GetComponentInChildren<PlayerProgressionComponent>();
            return progression != null;
        }
    }
}
