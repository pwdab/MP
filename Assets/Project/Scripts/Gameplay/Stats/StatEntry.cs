using System;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    [Serializable]
    public struct StatEntry
    {
        [SerializeField] private StatId statId;
        [SerializeField] private float baseValue;
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
    }
}
