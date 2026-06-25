using System;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    [Serializable]
    public struct StatEntry
    {
        [Tooltip("Stat represented by this entry.")]
        [SerializeField] private StatId statId;

        [Tooltip("Base value before item, job, skill, or buff modifiers are applied.")]
        [SerializeField] private float baseValue;

        [Tooltip("Minimum and maximum allowed final value after modifiers.")]
        [SerializeField] private StatBounds bounds;

        public StatEntry(StatId statId, float baseValue, StatBounds bounds)
        {
            this.statId = statId;
            this.baseValue = baseValue;
            this.bounds = bounds;
        }

        public StatId StatId => statId;
        public float BaseValue => baseValue;
        public StatBounds Bounds => bounds;

        public StatEntry Normalized()
        {
            StatBounds normalizedBounds = bounds.Normalized();
            return new StatEntry(statId, normalizedBounds.Clamp(baseValue), normalizedBounds);
        }
    }
}
