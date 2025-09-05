using CamundaProject.Application.Services.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
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

namespace CamundaProject.Application.Services.AccountOpening
{
    public class GenerateApplicationIdJobWorkerService : IHostedService
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly ILogger<GenerateApplicationIdJobWorkerService> _logger;
        private IJobWorker? _generateRequestIdWorker;

        // Constructor for dependency injection
        public GenerateApplicationIdJobWorkerService(
            IZeebeClient zeebeClient,
            ILogger<GenerateApplicationIdJobWorkerService> logger)
        {
            _zeebeClient = zeebeClient;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Create a Zeebe job worker
            _generateRequestIdWorker = _zeebeClient.NewWorker()
                .JobType("generate-applicaion-id") // Match this with your BPMN task type
                .Handler(HandleJob)
                .MaxJobsActive(5)
                .Name("generate-application-id-worker")
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromMinutes(1))
                .Open();

            _logger.LogInformation("Generate Application ID Job Worker started");
            return Task.CompletedTask;
        }

        private async void HandleJob(IJobClient client, IJob job)
        {
            _logger.LogInformation($"Handling job {job.Key}");

            try
            {
                // Generate a unique application ID (e.g., GUID)
                var applicationId = Guid.NewGuid().ToString();

                // Complete the job with the generated application ID
                await client.NewCompleteJobCommand(job.Key)
                    .Variables(JsonSerializer.Serialize(new { applicationId }))
                    .Send();

                _logger.LogInformation($"Generated application ID: {applicationId} for job {job.Key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to handle job {job.Key}");
                // Fail the job to allow retries
                await client.NewFailCommand(job.Key)
                    .Retries(job.Retries - 1)
                    .ErrorMessage(ex.Message)
                    .Send();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Dispose the worker when the service stops
            _generateRequestIdWorker?.Dispose();
            _logger.LogInformation("Generate Application ID Job Worker stopped");
            return Task.CompletedTask;
        }
    }
}