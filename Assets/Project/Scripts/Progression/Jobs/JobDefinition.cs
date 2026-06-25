using System;
using System.Collections.Generic;
using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Progression.Jobs
{
    [CreateAssetMenu(menuName = "MP/Data/Job Definition")]
    public sealed class JobDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable job identifier used by save data and future network snapshots.")]
        [SerializeField] private string jobId;

        [Tooltip("Name shown to players.")]
        [SerializeField] private string displayName;

        [Header("Tree")]
        [Tooltip("Broad job category this job belongs to.")]
        [SerializeField] private JobCategory category;

        [Tooltip("Parent job required before this job can be selected. Leave empty for root jobs.")]
        [SerializeField] private JobDefinition parentJob;

        [Header("Stats")]
        [Tooltip("Stat modifiers applied while this job is selected.")]
        [SerializeField] private StatModifierDefinition[] statModifiers;

        public string JobId => jobId;
        public string DisplayName => displayName;
        public JobCategory Category => category;
        public JobDefinition ParentJob => parentJob;
        public IReadOnlyList<StatModifierDefinition> StatModifiers => statModifiers ?? Array.Empty<StatModifierDefinition>();

        private void OnValidate()
        {
            jobId = jobId != null ? jobId.Trim() : string.Empty;
        }
    }
}
