using CamundaProject.Core.Models.CamundaModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Interfaces.Services
{
    public interface IJobTrackingService
    {
        void AddJob(ActiveJob job);
        void RemoveJob(long jobKey);
        ActiveJob GetJob(long jobKey);
        List<ActiveJob> GetJobsByProcessInstance(long processInstanceKey);
        List<ActiveJob> GetJobsByType(string jobType);
        List<ActiveJob> GetAllJobs();
        void UpdateJobStatus(long jobKey, string status);
    }

}