using System;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    [Serializable]
    public struct StatBounds
    {
        [Tooltip("Minimum allowed final value for this stat.")]
        [SerializeField] private float minimum;

        [Tooltip("Maximum allowed final value for this stat.")]
        [SerializeField] private float maximum;

        public StatBounds(float minimum, float maximum)
        {
            this.minimum = minimum;
            this.maximum = maximum;
        }

        public float Minimum => minimum;
        public float Maximum => Mathf.Max(minimum, maximum);
        public bool IsValid => minimum <= maximum;

        public StatBounds Normalized()
        {
            return new StatBounds(minimum, Maximum);
        }

        public float Clamp(float value)
        {
            return Mathf.Clamp(value, Minimum, Maximum);
        }
    }
}
