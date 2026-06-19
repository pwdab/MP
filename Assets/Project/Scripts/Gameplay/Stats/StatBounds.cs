using System;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    [Serializable]
    public struct StatBounds
    {
        [SerializeField] private float minimum;
        [SerializeField] private float maximum;

        public StatBounds(float minimum, float maximum)
        {
            this.minimum = minimum;
            this.maximum = maximum;
        }

        public float Minimum => minimum;
        public float Maximum => Mathf.Max(minimum, maximum);
        public bool IsValid => minimum <= maximum;

        public float Clamp(float value)
        {
            return Mathf.Clamp(value, Minimum, Maximum);
        }
    }
}
