using UnityEngine;

namespace MP.Progression.Level
{
    public sealed class PlayerProgressionComponent : MonoBehaviour
    {
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0)] private int experience;
        [SerializeField, Min(0)] private int remainingGrowthPoints;

        public int Level => level;
        public int Experience => experience;
        public int RemainingGrowthPoints => remainingGrowthPoints;

        public void AddExperience(int amount)
        {
            experience += Mathf.Max(0, amount);
        }

        public void AddGrowthPoints(int amount)
        {
            remainingGrowthPoints += Mathf.Max(0, amount);
        }

        public bool TrySpendGrowthPoint()
        {
            if (remainingGrowthPoints <= 0)
            {
                return false;
            }

            remainingGrowthPoints--;
            return true;
        }
    }
}
