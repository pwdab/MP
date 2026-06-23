using UnityEngine;
using MP.Gameplay.Stats;

namespace MP.Progression.Jobs
{
    [RequireComponent(typeof(StatsComponent))]
    public sealed class PlayerJobComponent : MonoBehaviour
    {
        [SerializeField] private JobDefinition currentJob;
        private StatsComponent stats;

        public JobDefinition CurrentJob => currentJob;
        public JobCategory CurrentCategory => currentJob != null ? currentJob.Category : JobCategory.None;
        public string CurrentJobId => currentJob != null ? currentJob.JobId : string.Empty;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            ApplyCurrentJobModifiers();
        }

        public void SetJob(JobDefinition job)
        {
            if (currentJob == job)
            {
                return;
            }

            stats ??= GetComponent<StatsComponent>();
            stats.RemoveModifiersFrom(this);
            currentJob = job;
            ApplyCurrentJobModifiers();
        }

        private void ApplyCurrentJobModifiers()
        {
            if (currentJob == null)
            {
                return;
            }

            stats ??= GetComponent<StatsComponent>();
            stats.AddModifiers(currentJob.StatModifiers, this);
        }
    }
}
