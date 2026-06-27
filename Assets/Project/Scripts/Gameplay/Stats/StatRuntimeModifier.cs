namespace MP.Gameplay.Stats
{
    /*
        런타임에 실제 적용되는 스탯 Modifier
        제거와 디버깅을 위해 어떤 source에서 온 Modifier인지 함께 보관
        source는 StatModifierSource로 감싸며 Owner는 안정적인 참조 타입이어야 함
    */
    public readonly struct StatRuntimeModifier
    {
        public StatRuntimeModifier(StatId statId, StatModifierType type, float value, StatModifierSource source)
        {
            StatId = statId;
            Type = type;
            Value = value;
            Source = source;

            ValidateOrThrow();
        }

        public StatId StatId { get; }
        public StatModifierType Type { get; }
        public float Value { get; }
        public StatModifierSource Source { get; }

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (!System.Enum.IsDefined(typeof(StatId), StatId))
            {
                reason = $"StatRuntimeModifier has invalid StatId '{StatId}'.";
                return false;
            }

            if (!System.Enum.IsDefined(typeof(StatModifierType), Type))
            {
                reason = $"StatRuntimeModifier has invalid type '{Type}'.";
                return false;
            }

            if (float.IsNaN(Value) || float.IsInfinity(Value))
            {
                reason = $"StatRuntimeModifier '{StatId}' has invalid value '{Value}'.";
                return false;
            }

            if (!Source.IsValid())
            {
                reason = "StatRuntimeModifier has invalid source.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid(out string reason))
            {
                throw new System.InvalidOperationException(reason);
            }
        }
    }
}
