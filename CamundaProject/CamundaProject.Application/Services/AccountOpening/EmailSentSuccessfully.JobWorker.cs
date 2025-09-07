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
    public class EmailSentSuccessfullyJobWorker :IHostedService
    {
        private readonly ILogger<EmailSentSuccessfullyJobWorker> _logger;
        private readonly IZeebeClient _zeebeClient;
        private IJobWorker? _successHandlerJobWorker;

        public EmailSentSuccessfullyJobWorker(IZeebeClient zeebeClient,ILogger<EmailSentSuccessfullyJobWorker> logger)
        {
            _zeebeClient = zeebeClient;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Email Success job worker...");

            _successHandlerJobWorker = _zeebeClient.NewWorker()
                .JobType("email-success")
                .Handler(async (client, job) => await HandleSuccessJobAsync(client, job))
                .MaxJobsActive(5)
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            _logger.LogInformation("Email Success job worker started successfully");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Email Success job worker...");
            _successHandlerJobWorker?.Dispose();
            _logger.LogInformation("Email Success job worker stopped");
            return Task.CompletedTask;
        }
        public async Task HandleSuccessJobAsync(IJobClient client, IJob job)
        {
            try
            {
                _logger.LogInformation("Processing success handler job {JobKey}", job.Key);

                // Extract response data from variables
                var variables = job.Variables;
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(variables);

                var applicationId = jsonElement.TryGetProperty("applicationId", out var applicationIdProp)
                    ? applicationIdProp.GetString()
                    : "unknown";

                var responseData = jsonElement.TryGetProperty("response", out var responseProp)
                    ? responseProp.ToString()
                    : "{}";

                // Log success and perform any success-related operations
                _logger.LogInformation("Application {applicationId} completed successfully. Response: {Response}",
                    applicationId, responseData);

                // You can add additional success handling logic here
                // For example: update database, send notifications, etc.

                await client.NewCompleteJobCommand(job.Key)
                    .Send();

                _logger.LogInformation("Success handler job {JobKey} completed", job.Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in success handler job {JobKey}", job.Key);
                await client.NewFailCommand(job.Key)
                    .Retries(job.Retries - 1)
                    .ErrorMessage(ex.Message)
                    .Send();
            }
        }

 
    }
}
