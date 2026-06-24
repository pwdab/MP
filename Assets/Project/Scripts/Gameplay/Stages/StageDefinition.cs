using UnityEngine;

namespace MP.Gameplay.Stages
{
    [CreateAssetMenu(menuName = "MP/Stages/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [SerializeField] private string stageId = "stage";
        [SerializeField] private string displayName = "Stage";
        [SerializeField] private int startingGold;
        [SerializeField] private int startingExperience;
        [SerializeField] private WaveDefinition[] waves;

        public string StageId => stageId;
        public string DisplayName => displayName;
        public int StartingGold => Mathf.Max(0, startingGold);
        public int StartingExperience => Mathf.Max(0, startingExperience);
        public WaveDefinition[] Waves => waves;
        public int WaveCount => waves != null ? waves.Length : 0;

        public bool TryGetWave(int index, out WaveDefinition wave)
        {
            if (waves == null || index < 0 || index >= waves.Length)
            {
                wave = null;
                return false;
            }

            wave = waves[index];
            return wave != null;
        }
    }
}
