using UnityEngine;

namespace MP.Gameplay.Stages
{
    [CreateAssetMenu(menuName = "MP/Stages/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [Header("Stage")]
        [Tooltip("Stable stage identifier used by save data or future content references.")]
        [SerializeField] private string stageId = "stage";

        [Tooltip("Name shown in UI and editor summaries.")]
        [SerializeField] private string displayName = "Stage";

        [Min(0)]
        [Tooltip("Gold granted when the stage starts.")]
        [SerializeField] private int startingGold;

        [Min(0)]
        [Tooltip("Experience granted when the stage starts.")]
        [SerializeField] private int startingExperience;

        [Header("Waves")]
        [Tooltip("Ordered wave list. The first entry starts immediately when the stage begins.")]
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

        private void OnValidate()
        {
            startingGold = Mathf.Max(0, startingGold);
            startingExperience = Mathf.Max(0, startingExperience);

            if (waves == null)
            {
                return;
            }

            for (int i = 0; i < waves.Length; i++)
            {
                waves[i]?.Validate();
            }
        }
    }
}
