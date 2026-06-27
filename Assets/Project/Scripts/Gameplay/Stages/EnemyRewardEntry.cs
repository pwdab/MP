using System;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    [Serializable]
    public sealed class EnemyRewardEntry
    {
        [SerializeField] private string enemyId;
        [SerializeField, Min(0)] private int experienceReward;

        public string EnemyId => enemyId ?? string.Empty;
        public int ExperienceReward => Mathf.Max(0, experienceReward);
    }
}
