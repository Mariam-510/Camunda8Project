using CamundaProject.Core.Interfaces.Services;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace CamundaProject.Application.Services.Zeebe
{
    public class ZeebeJobWorkerService : IHostedService
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly ILogger<ZeebeJobWorkerService> _logger;
        private IJobWorker? _testTaskWorker;

        public ZeebeJobWorkerService(
            IZeebeClient zeebeClient,
            ILogger<ZeebeJobWorkerService> logger)
        {
            _zeebeClient = zeebeClient;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Zeebe job workers...");

            // Create worker for tasks (type should match your BPMN)
            _testTaskWorker = _zeebeClient.NewWorker()
                .JobType("test_job") // This must match your BPMN task type
                .Handler(HandleTestTaskJob)
                .MaxJobsActive(10)
                //.Name("UserTaskWorker")
                .Name(Environment.MachineName)
                .AutoCompletion()
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            _logger.LogInformation("Zeebe job workers started successfully");
            return Task.CompletedTask;
        }

        private async Task HandleTestTaskJob(IJobClient jobClient, IJob job)
        {
            try
            {
                _logger.LogInformation("New task job received: {JobKey}, Process: {ProcessInstanceKey}",
                    job.Key, job.ProcessInstanceKey);

                // Parse variables from the job
                var variables = string.IsNullOrEmpty(job.Variables)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(job.Variables);


                _logger.LogInformation("task job {JobKey} completed via API", job.Key);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling task job: {JobKey}", job.Key);

                // Fail the job if there's an error in handling
                await jobClient.NewFailCommand(job.Key)
                    .Retries(job.Retries - 1)
                    .ErrorMessage($"Error handling job: {ex.Message}")
                    .Send();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _testTaskWorker?.Dispose();
            _logger.LogInformation("Zeebe job workers stopped");
            return Task.CompletedTask;
        }
    }
}