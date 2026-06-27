using System;
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

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(stageId))
            {
                reason = $"{name} has an empty stage id.";
                return false;
            }

            if (startingGold < 0)
            {
                reason = $"{name} has invalid starting gold '{startingGold}'.";
                return false;
            }

            if (startingExperience < 0)
            {
                reason = $"{name} has invalid starting experience '{startingExperience}'.";
                return false;
            }

            if (waves == null || waves.Length == 0)
            {
                reason = $"{name} has no waves.";
                return false;
            }

            for (int i = 0; i < waves.Length; i++)
            {
                WaveDefinition wave = waves[i];
                if (wave == null)
                {
                    reason = $"{name} has an empty wave at index {i}.";
                    return false;
                }

                if (!wave.IsValid(out string waveReason))
                {
                    reason = $"{name} wave {i} is invalid: {waveReason}";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid(out string reason))
            {
                throw new InvalidOperationException(reason);
            }
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
                waves[i]?.Normalize();
            }
        }
    }
}
