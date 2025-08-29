using CamundaProject.Core.Interfaces.Services;
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

namespace CamundaProject.Application.Services.Kafka
{
    public class KafkaJobWorkerService : IHostedService
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly ILogger<KafkaJobWorkerService> _logger;
        private readonly IProducer<string, string> _kafkaProducer;
        private IJobWorker? _generateRequestIdWorker;
        private IJobWorker? _kafkaWorker;
        private IJobWorker? _successWorker;
        private IJobWorker? _errorWorker;
        private readonly string _topic;

        public KafkaJobWorkerService(
            IZeebeClient zeebeClient,
            ILogger<KafkaJobWorkerService> logger,
            IProducer<string, string> kafkaProducer,
            IConfiguration configuration)
        {
            _zeebeClient = zeebeClient;
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _topic = configuration["Kafka:Topic"];
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Zeebe job workers...");

            _generateRequestIdWorker = _zeebeClient.NewWorker()
                .JobType("generate-request-id")
                .Handler(HandleGenerateRequestIdJobAsync)
                .AutoCompletion()
                .MaxJobsActive(5)
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            _kafkaWorker = _zeebeClient.NewWorker()
                .JobType("kafka-publish")
                .Handler(HandleKafkaJobAsync)
                .AutoCompletion()
                .MaxJobsActive(5)
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            _successWorker = _zeebeClient.NewWorker()
                .JobType("success-handler")
                .Handler(HandleSuccessJobAsync)
                .AutoCompletion()
                .MaxJobsActive(5)
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            _errorWorker = _zeebeClient.NewWorker()
                .JobType("error-handler")
                .Handler(HandleErrorJobAsync)
                .AutoCompletion()
                .MaxJobsActive(5)
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            _logger.LogInformation("Zeebe job workers started successfully");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _generateRequestIdWorker?.Dispose();
            _kafkaWorker?.Dispose();
            _successWorker?.Dispose();
            _errorWorker?.Dispose();
            _logger.LogInformation("Zeebe job workers stopped");
            return Task.CompletedTask;
        }

        private async Task HandleGenerateRequestIdJobAsync(IJobClient client, IJob job)
        {
            try
            {
                _logger.LogInformation("Generating request ID for job {JobKey}", job.Key);

                // Generate a unique request ID
                var requestId = Guid.NewGuid().ToString();

                // Complete the job with the generated request ID
                await client.NewCompleteJobCommand(job.Key)
                    .Variables(JsonSerializer.Serialize(new
                    {
                        requestId
                    }))
                    .Send();

                _logger.LogInformation("Generated request ID: {RequestId} for job {JobKey}", requestId, job.Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating request ID for job {JobKey}", job.Key);
                await client.NewFailCommand(job.Key)
                    .Retries(job.Retries - 1)
                    .ErrorMessage(ex.Message)
                    .Send();
            }
        }

        private async Task HandleKafkaJobAsync(IJobClient client, IJob job)
        {
            try
            {
                _logger.LogInformation("Processing Kafka publish job {JobKey}", job.Key);

                // Extract variables from the job
                var variables = job.Variables;
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(variables);

                var requestId = jsonElement.GetProperty("requestId").GetString();
                var to = jsonElement.GetProperty("to").GetString();
                var subject = jsonElement.GetProperty("subject").GetString();
                var body = jsonElement.GetProperty("body").GetString();

                if (string.IsNullOrEmpty(requestId))
                    throw new ArgumentException("Request ID is required");


                if (string.IsNullOrEmpty(to))
                    throw new ArgumentException("To is required");

                // Prepare Kafka message with email details
                var kafkaMessage = new
                {
                    RequestId = requestId,
                    To = to,
                    Subject = subject,
                    Body = body,
                    Status = "pending",
                    Timestamp = DateTime.UtcNow
                };

                var messageJson = JsonSerializer.Serialize(kafkaMessage);

                // Publish to Kafka
                var deliveryResult = await _kafkaProducer.ProduceAsync(
                    _topic,
                    new Message<string, string>
                    {
                        Key = requestId,
                        Value = messageJson
                    });

                _logger.LogInformation("Message published to Kafka. Topic: {Topic}", _topic);

                await client.NewCompleteJobCommand(job.Key).Send();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Kafka job {JobKey}", job.Key);
                await client.NewFailCommand(job.Key)
                    .Retries(job.Retries - 1)
                    .ErrorMessage(ex.Message)
                    .Send();
            }
        }

        private async Task HandleSuccessJobAsync(IJobClient client, IJob job)
        {
            try
            {
                _logger.LogInformation("Processing success handler job {JobKey}", job.Key);

                // Extract response data from variables
                var variables = job.Variables;
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(variables);

                var requestId = jsonElement.TryGetProperty("requestId", out var requestIdProp)
                    ? requestIdProp.GetString()
                    : "unknown";

                var responseData = jsonElement.TryGetProperty("response", out var responseProp)
                    ? responseProp.ToString()
                    : "{}";

                // Log success and perform any success-related operations
                _logger.LogInformation("Request {RequestId} completed successfully. Response: {Response}",
                    requestId, responseData);

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

        private async Task HandleErrorJobAsync(IJobClient client, IJob job)
        {
            try
            {
                _logger.LogInformation("Processing error handler job {JobKey}", job.Key);

                // Extract error information from variables
                var variables = job.Variables;
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(variables);

                var requestId = jsonElement.TryGetProperty("requestId", out var requestIdProp)
                    ? requestIdProp.GetString()
                    : "unknown";

                var errorMessage = jsonElement.TryGetProperty("error", out var errorProp)
                    ? errorProp.GetString()
                    : "Unknown error occurred";

                // Log error and perform error handling operations
                _logger.LogError("Request {RequestId} failed with error: {ErrorMessage}",
                    requestId, errorMessage);

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