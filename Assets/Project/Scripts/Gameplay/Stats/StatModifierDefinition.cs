using UnityEngine;

namespace MP.Gameplay.Stats
{
    public enum StatModifierType
    {
        Flat,
        Percent
    }

    [CreateAssetMenu(menuName = "MP/Data/Stat Modifier Definition")]
    public sealed class StatModifierDefinition : ScriptableObject
    {
        [Header("Modifier")]
        [Tooltip("Stat affected by this modifier.")]
        [SerializeField] private StatId statId;

        [Tooltip("Flat adds value directly. Percent adds value as a ratio, for example 0.2 means +20%.")]
        [SerializeField] private StatModifierType type;

        [Tooltip("Modifier amount. Percent values are ratios, not 0-100 percentages.")]
        [SerializeField] private float value;

        public StatId StatId => statId;
        public StatModifierType Type => type;
        public float Value => value;

        public StatRuntimeModifier CreateRuntimeModifier(object source)
        {
            return new StatRuntimeModifier(statId, type, value, source);
        }
    }
}
