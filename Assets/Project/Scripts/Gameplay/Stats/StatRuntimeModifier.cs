namespace MP.Gameplay.Stats
{
    public readonly struct StatRuntimeModifier
    {
        public StatRuntimeModifier(StatId statId, StatModifierType type, float value, object source)
        {
            if (source == null)
            {
                throw new System.ArgumentNullException(nameof(source));
            }

            StatId = statId;
            Type = type;
            Value = value;
            Source = source;
        }

        public StatId StatId { get; }
        public StatModifierType Type { get; }
        public float Value { get; }
        public object Source { get; }
    }
}
