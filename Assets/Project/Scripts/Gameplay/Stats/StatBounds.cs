using System;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    /*
        하나의 스탯이 가질 수 있는 최소값과 최대값을 정의
        스탯 값이 최소값과 최대값 사이에 항상 존재하도록 보장
        최대값이 최소값보다 작게 입력되면 검증 단계에서 최소값 기준으로 보정
    */
    [Serializable]
    public struct StatBounds
    {
        [Tooltip("이 스탯이 가질 수 있는 최소 최종값입니다.")]
        [SerializeField] private float minimum;

        [Tooltip("이 스탯이 가질 수 있는 최대 최종값입니다.")]
        [SerializeField] private float maximum;

        public StatBounds(float minimum, float maximum)
        {
            this.minimum = minimum;
            this.maximum = maximum;
        }

        public float Minimum => minimum;
  
        public float Maximum => maximum;

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (float.IsNaN(minimum) || float.IsInfinity(minimum) || float.IsNaN(maximum) || float.IsInfinity(maximum))
            {
                reason = "StatBounds contains NaN or Infinity.";
                return false;
            }

            if (minimum > maximum)
            {
                reason = "StatBounds minimum is greater than maximum.";
                return false;
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

        public StatBounds WithValidRange()
        {
            return new StatBounds(minimum, Mathf.Max(minimum, maximum));
        }

        public float Clamp(float value)
        {
            return Mathf.Clamp(value, Minimum, Maximum);
        }
    }
}
