using System;
using System.Runtime.CompilerServices;

namespace MP.Gameplay.Stats
{
    /*
        스탯 Modifier의 출처 종류
        디버그 UI나 로그에서 장비, 직업, 버프, 강화처럼 출처를 구분하기 위해 사용
        실제 적용 코드에서는 Unspecified 대신 구체적인 출처 종류를 사용해야 함
    */
    public enum StatModifierSourceType
    {
        Unspecified,
        Equipment,
        Job,
        Buff,
        Skill,
        Upgrade,
        System
    }

    /*
        스탯 Modifier의 출처 정보
        equality는 DisplayName이 아니라 Owner 참조와 SourceType을 기준으로 판단
        Owner는 장비 인스턴스, 버프 인스턴스, 컴포넌트처럼 안정적인 참조 타입이어야 함
    */
    public readonly struct StatModifierSource : IEquatable<StatModifierSource>
    {
        private readonly object owner;
        private readonly string displayName;
        private readonly StatModifierSourceType sourceType;

        public StatModifierSource(object owner, StatModifierSourceType sourceType, string displayName = null)
        {
            ValidateOwner(owner);
            ValidateSourceType(sourceType);

            this.owner = owner;
            this.sourceType = sourceType;
            this.displayName = displayName;
        }

        public object Owner => owner;
        public StatModifierSourceType SourceType => sourceType;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? GetFallbackDisplayName() : displayName;

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (owner == null)
            {
                reason = "StatModifierSource owner is missing.";
                return false;
            }

            if (sourceType == StatModifierSourceType.Unspecified || !Enum.IsDefined(typeof(StatModifierSourceType), sourceType))
            {
                reason = $"StatModifierSource has invalid source type '{sourceType}'.";
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

        public bool Equals(StatModifierSource other)
        {
            return ReferenceEquals(owner, other.owner) && sourceType == other.sourceType;
        }

        public override bool Equals(object obj)
        {
            return obj is StatModifierSource other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int ownerHash = owner != null ? RuntimeHelpers.GetHashCode(owner) : 0;
                return (ownerHash * 397) ^ (int)sourceType;
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public static bool operator ==(StatModifierSource left, StatModifierSource right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StatModifierSource left, StatModifierSource right)
        {
            return !left.Equals(right);
        }

        private static void ValidateOwner(object owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            Type ownerType = owner.GetType();
            if (ownerType.IsValueType || owner is string)
            {
                throw new ArgumentException("Modifier source owner must be a stable reference object.", nameof(owner));
            }
        }

        private static void ValidateSourceType(StatModifierSourceType sourceType)
        {
            if (sourceType == StatModifierSourceType.Unspecified || !Enum.IsDefined(typeof(StatModifierSourceType), sourceType))
            {
                throw new ArgumentException("Modifier source type must be specified.", nameof(sourceType));
            }
        }

        private string GetFallbackDisplayName()
        {
            return owner != null ? owner.ToString() : "Invalid Modifier Source";
        }
    }
}
