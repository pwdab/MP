using UnityEngine;
using Unity.Netcode;

namespace MP.Progression.Level
{
    public sealed class PlayerProgressionComponent : NetworkBehaviour
    {
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0)] private int experience;
        [SerializeField, Min(0)] private int remainingGrowthPoints;

        private readonly NetworkVariable<int> networkLevel = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> networkExperience = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> networkRemainingGrowthPoints = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public int Level => IsSpawned ? networkLevel.Value : level;
        public int Experience => IsSpawned ? networkExperience.Value : experience;
        public int RemainingGrowthPoints => IsSpawned ? networkRemainingGrowthPoints.Value : remainingGrowthPoints;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            networkLevel.Value = Mathf.Max(1, level);
            networkExperience.Value = Mathf.Max(0, experience);
            networkRemainingGrowthPoints.Value = Mathf.Max(0, remainingGrowthPoints);
        }

        public void AddExperience(int amount)
        {
            int clampedAmount = Mathf.Max(0, amount);
            experience += clampedAmount;
            if (IsSpawned && IsServer)
            {
                networkExperience.Value += clampedAmount;
            }
        }

        public void AddGrowthPoints(int amount)
        {
            int clampedAmount = Mathf.Max(0, amount);
            remainingGrowthPoints += clampedAmount;
            if (IsSpawned && IsServer)
            {
                networkRemainingGrowthPoints.Value += clampedAmount;
            }
        }

        public bool TrySpendGrowthPoint()
        {
            if (RemainingGrowthPoints <= 0)
            {
                return false;
            }

            remainingGrowthPoints--;
            if (IsSpawned && IsServer)
            {
                networkRemainingGrowthPoints.Value = Mathf.Max(0, networkRemainingGrowthPoints.Value - 1);
            }

            return true;
        }
    }
}
