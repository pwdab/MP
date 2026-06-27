using UnityEngine;
using System;

namespace MP.Gameplay.Stats
{
    /*
        Modifier 계산 방식 정의
        Flat은 기본값에 더해지고, Percent는 Flat 적용 후 곱해 적용
    */
    public enum StatModifierType
    {
        Flat,
        Percent
    }

    /*
        정적 스탯 Modifier 데이터
        아이템, 직업, 스킬, 버프 등에서 재사용하며 런타임 적용 시 StatRuntimeModifier로 변환
        Percent 값은 0.5가 +50%, -0.1이 -10%를 의미
    */
    [CreateAssetMenu(menuName = "MP/Data/Stat Modifier Definition")]
    public sealed class StatModifierDefinition : ScriptableObject
    {
        [Header("스탯 Modifier 설정")]
        [Tooltip("Modifier를 적용할 스탯 종류.")]
        [SerializeField] private StatId statId;

        [Tooltip("Modifier 적용 방식. Flat은 합연산, Percent는 곱연산.")]
        [SerializeField] private StatModifierType type;

        [Tooltip("Modifier 값. Flat은 그대로 더해지는 값, Percent는 0.5 입력 시 +50%, -0.1 입력 시 -10%.")]
        [SerializeField] private float value;

        public StatId StatId => statId;
        public StatModifierType Type => type;
        public float Value => value;

        private void OnValidate()
        {
            if (!IsValid(out string reason))
            {
                Debug.LogWarning(reason, this);
            }
        }

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (!Enum.IsDefined(typeof(StatId), statId))
            {
                reason = $"{name} has invalid StatId '{statId}'.";
                return false;
            }

            if (!Enum.IsDefined(typeof(StatModifierType), type))
            {
                reason = $"{name} has invalid modifier type '{type}'.";
                return false;
            }

            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                reason = $"{name} has invalid modifier value '{value}'.";
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

        public StatRuntimeModifier CreateRuntimeModifier(StatModifierSource source)
        {
            ValidateOrThrow();
            return new StatRuntimeModifier(statId, type, value, source);
        }
    }
}
