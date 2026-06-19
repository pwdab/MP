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
        [SerializeField] private StatId statId;
        [SerializeField] private StatModifierType type;
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
