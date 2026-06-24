using UnityEngine;

namespace MP.Gameplay.Stages
{
    public sealed class WaveEnemyComponent : MonoBehaviour
    {
        public int WaveIndex { get; private set; } = -1;
        public bool IsBoss { get; private set; }

        public void Initialize(int waveIndex, bool isBoss)
        {
            WaveIndex = waveIndex;
            IsBoss = isBoss;
        }
    }
}
