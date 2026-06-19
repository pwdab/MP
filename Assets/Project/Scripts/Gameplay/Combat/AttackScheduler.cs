using UnityEngine;

namespace MP.Gameplay.Combat
{
    public sealed class AttackScheduler
    {
        private float attackAccumulator;

        public int Tick(float deltaTime, float attacksPerSecond)
        {
            if (deltaTime <= 0f || attacksPerSecond <= 0f)
            {
                return 0;
            }

            attackAccumulator += deltaTime * attacksPerSecond;

            int attackCount = Mathf.FloorToInt(attackAccumulator);
            if (attackCount <= 0)
            {
                return 0;
            }

            attackAccumulator -= attackCount;
            return attackCount;
        }

        public void Reset()
        {
            attackAccumulator = 0f;
        }
    }
}
