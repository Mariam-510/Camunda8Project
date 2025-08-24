using CamundaProject.Core.Interfaces.Services;
using CamundaProject.Core.Models.CamundaModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Application.Services
{
    public class JobTrackingService : IJobTrackingService
    {
        private readonly ConcurrentDictionary<long, ActiveJob> _activeJobs = new();
        private readonly ILogger<JobTrackingService> _logger;

        public JobTrackingService(ILogger<JobTrackingService> logger)
        {
            _logger = logger;
        }

        public void AddJob(ActiveJob job)
        {
            _activeJobs[job.JobKey] = job;
            _logger.LogInformation("Added job {JobKey} to tracking", job.JobKey);
        }

        public void RemoveJob(long jobKey)
        {
            _activeJobs.TryRemove(jobKey, out _);
            _logger.LogInformation("Removed job {JobKey} from tracking", jobKey);
        }

        public ActiveJob GetJob(long jobKey)
        {
            return _activeJobs.TryGetValue(jobKey, out var job) ? job : null;
        }

        public List<ActiveJob> GetJobsByProcessInstance(long processInstanceKey)
        {
            return _activeJobs.Values
                .Where(j => j.ProcessInstanceKey == processInstanceKey)
                .ToList();
        }

        public List<ActiveJob> GetJobsByType(string jobType)
        {
            return _activeJobs.Values
                .Where(j => j.JobType == jobType)
                .ToList();
        }

        public List<ActiveJob> GetAllJobs()
        {
            return _activeJobs.Values.ToList();
        }

        public void UpdateJobStatus(long jobKey, string status)
        {
            if (_activeJobs.TryGetValue(jobKey, out var job))
            {
                job.Status = status;
                _logger.LogInformation("Updated job {JobKey} status to {Status}", jobKey, status);
            }
        }
    }

}
