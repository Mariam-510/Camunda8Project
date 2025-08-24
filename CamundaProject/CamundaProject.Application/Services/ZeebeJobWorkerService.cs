using CamundaProject.Core.Interfaces.Services;
using CamundaProject.Core.Models.CamundaModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;
using System.Text.Json;

namespace CamundaProject.Application.Services
{
    public class ZeebeJobWorkerService : IHostedService
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly IJobTrackingService _jobTrackingService;
        private readonly ILogger<ZeebeJobWorkerService> _logger;
        private IJobWorker? _userTaskWorker;

        public ZeebeJobWorkerService(
            IZeebeClient zeebeClient,
            IJobTrackingService jobTrackingService,
            ILogger<ZeebeJobWorkerService> logger)
        {
            _zeebeClient = zeebeClient;
            _jobTrackingService = jobTrackingService;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Zeebe job workers...");

            // Create worker for user tasks (type should match your BPMN)
            _userTaskWorker = _zeebeClient.NewWorker()
                .JobType("test_job") // This must match your BPMN task type
                .Handler(HandleUserTaskJob)
                .MaxJobsActive(10)
                //.Name("UserTaskWorker")
                //.PollInterval(TimeSpan.FromSeconds(1))
                //.Timeout(TimeSpan.FromMinutes(30))
                .Name(Environment.MachineName)
                .AutoCompletion()
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            _logger.LogInformation("Zeebe job workers started successfully");
            return Task.CompletedTask;
        }

        private async Task HandleUserTaskJob(IJobClient jobClient, IJob job)
        {
            try
            {
                _logger.LogInformation("New user task job received: {JobKey}, Process: {ProcessInstanceKey}",
                    job.Key, job.ProcessInstanceKey);

                //// Parse variables from the job
                //var variables = string.IsNullOrEmpty(job.Variables)
                //    ? new Dictionary<string, object>()
                //    : JsonSerializer.Deserialize<Dictionary<string, object>>(job.Variables);

                //// Track the job so it can be completed via API
                //var activeJob = new ActiveJob
                //{
                //    JobKey = job.Key,
                //    ProcessInstanceKey = job.ProcessInstanceKey,
                //    JobType = job.Type,
                //    ElementId = job.ElementId,
                //    Variables = variables,
                //    CreatedAt = DateTime.UtcNow,
                //    Status = "Active"
                //};

                //_jobTrackingService.AddJob(activeJob);

                _logger.LogInformation("User task job {JobKey} is waiting for completion via API", job.Key);

                // Job remains active until completed via API call
                // Don't complete it here - wait for external completion

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user task job: {JobKey}", job.Key);

                // Fail the job if there's an error in handling
                await jobClient.NewFailCommand(job.Key)
                    .Retries(job.Retries - 1)
                    .ErrorMessage($"Error handling job: {ex.Message}")
                    .Send();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _userTaskWorker?.Dispose();
            _logger.LogInformation("Zeebe job workers stopped");
            return Task.CompletedTask;
        }
    }
}