using UnityEngine;

namespace MP.Progression.Jobs
{
    public sealed class PlayerJobComponent : MonoBehaviour
    {
        [SerializeField] private JobDefinition currentJob;

        public JobDefinition CurrentJob => currentJob;
        public JobCategory CurrentCategory => currentJob != null ? currentJob.Category : JobCategory.None;
        public string CurrentJobId => currentJob != null ? currentJob.JobId : string.Empty;

        public void SetJob(JobDefinition job)
        {
            currentJob = job;
        }
    }
}
