using UnityEngine;

namespace MP.Progression.Level
{
    /*
        플레이어 성장 데이터
        레벨, 경험치, 성장 포인트를 관리하며 네트워크 동기화는 별도 어댑터가 담당
    */
    public sealed class PlayerProgressionComponent : MonoBehaviour
    {
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0)] private int experience;
        [SerializeField, Min(0)] private int remainingGrowthPoints;

        public int Level => Mathf.Max(1, level);
        public int Experience => Mathf.Max(0, experience);
        public int RemainingGrowthPoints => Mathf.Max(0, remainingGrowthPoints);

        public void AddExperience(int amount)
        {
            experience = Mathf.Max(0, experience + Mathf.Max(0, amount));
        }

        public void AddGrowthPoints(int amount)
        {
            remainingGrowthPoints = Mathf.Max(0, remainingGrowthPoints + Mathf.Max(0, amount));
        }

        public bool TrySpendGrowthPoint()
        {
            if (RemainingGrowthPoints <= 0)
            {
                return false;
            }

            remainingGrowthPoints = Mathf.Max(0, remainingGrowthPoints - 1);
            return true;
        }

        public void ApplySnapshot(int snapshotLevel, int snapshotExperience, int snapshotGrowthPoints)
        {
            level = Mathf.Max(1, snapshotLevel);
            experience = Mathf.Max(0, snapshotExperience);
            remainingGrowthPoints = Mathf.Max(0, snapshotGrowthPoints);
        }
    }
}
