using System;
using MP.Gameplay.Events;
using MP.Progression.Level;
using MP.UI;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    public sealed class EnemyRewardSystem : MonoBehaviour
    {
        [SerializeField] private EnemyRewardEntry[] rewardEntries;

        public void GrantReward(EnemyKilledEvent eventData)
        {
            if (!TryGetExperienceReward(eventData.EnemyId, out int experienceReward))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"{nameof(EnemyRewardSystem)} has no reward entry for enemy id '{eventData.EnemyId}'.", this);
#endif
                return;
            }

            if (experienceReward <= 0)
            {
                return;
            }

            GameObject rewardReceiver = eventData.DamageContext.RewardReceiver;
            if (!TryGetRewardReceiverProgression(rewardReceiver, out PlayerProgressionComponent progression))
            {
                return;
            }

            progression.AddExperience(experienceReward);
            FloatingWorldText.Show(eventData.DeathPosition + Vector3.up, $"+{experienceReward} EXP", new Color(0.35f, 0.8f, 1f, 1f));
        }

        private bool TryGetExperienceReward(string enemyId, out int experienceReward)
        {
            experienceReward = 0;
            if (string.IsNullOrWhiteSpace(enemyId) || rewardEntries == null)
            {
                return false;
            }

            for (int i = 0; i < rewardEntries.Length; i++)
            {
                EnemyRewardEntry entry = rewardEntries[i];
                if (entry != null && string.Equals(entry.EnemyId, enemyId, StringComparison.Ordinal))
                {
                    experienceReward = entry.ExperienceReward;
                    return true;
                }
            }

            return false;
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

