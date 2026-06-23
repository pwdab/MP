using UnityEngine;
using MP.Gameplay.Stats;

namespace MP.Progression.Jobs
{
    [CreateAssetMenu(menuName = "MP/Data/Job Definition")]
    public sealed class JobDefinition : ScriptableObject
    {
        [SerializeField] private string jobId;
        [SerializeField] private string displayName;
        [SerializeField] private JobCategory category;
        [SerializeField] private JobDefinition parentJob;
        [SerializeField] private StatModifierDefinition[] statModifiers;

        public string JobId => jobId;
        public string DisplayName => displayName;
        public JobCategory Category => category;
        public JobDefinition ParentJob => parentJob;
        public System.Collections.Generic.IReadOnlyList<StatModifierDefinition> StatModifiers => statModifiers;
    }
}
