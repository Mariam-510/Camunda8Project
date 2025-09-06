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
    public class EmailDeliveryFailedJobWorker:IHostedService
    {
        private readonly ILogger<EmailDeliveryFailedJobWorker> _logger;
        private readonly IZeebeClient _zeebeClient;
        private IJobWorker? _errorHandlerJobWorker;
        public EmailDeliveryFailedJobWorker(IZeebeClient zeebeClient, ILogger<EmailDeliveryFailedJobWorker> logger)
        {
            _zeebeClient = zeebeClient;
            _logger = logger;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Email Failed job worker...");

            _errorHandlerJobWorker = _zeebeClient.NewWorker()
                .JobType("email-failed")
                .Handler(async (client, job) => await HandleErrorJobAsync(client, job))
                .MaxJobsActive(5)
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            _logger.LogInformation("Email Failed job worker started successfully");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Email Failed job worker...");
            _errorHandlerJobWorker?.Dispose();
            _logger.LogInformation("Email Failed job worker stopped");
            return Task.CompletedTask;
        }

        private async Task HandleErrorJobAsync(IJobClient client, IJob job)
        {
            try
            {
                _logger.LogInformation("Processing error handler job {JobKey}", job.Key);

                // Extract error information from variables
                var variables = job.Variables;
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(variables);

                var applicationId = jsonElement.TryGetProperty("ApplicationId", out var applicationIdProp)
                    ? applicationIdProp.GetString()
                    : "unknown";

                var errorMessage = jsonElement.TryGetProperty("error", out var errorProp)
                    ? errorProp.GetString()
                    : "Unknown error occurred";

                // Log error and perform error handling operations
                _logger.LogError("Request {RequestId} failed with error: {ErrorMessage}",
                    applicationId, errorMessage);

                // You can add additional error handling logic here
                // For example: send alerts, update error logs, trigger compensation, etc.

                await client.NewCompleteJobCommand(job.Key)
                    .Send();

                _logger.LogInformation("Error handler job {JobKey} completed", job.Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in error handler job {JobKey}", job.Key);
                // Even if error handling fails, we complete the job to avoid infinite loops
                await client.NewCompleteJobCommand(job.Key)
                    .Send();
            }
        }
    }
}
