using CamundaProject.Application.Services.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
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
    public class SendEmailJobWorker : IHostedService
    {
        private readonly ILogger<SendEmailJobWorker> _logger;
        private readonly IZeebeClient _zeebeClient;
        private readonly IProducer<string, string> _kafkaProducer;
        private readonly SendEmailJobWorker _sendEmailWorker;
        private readonly string _topic;
        private IJobWorker? _sendEmailJobWorker;
        private readonly IConfiguration _configuration;


        public SendEmailJobWorker(IZeebeClient zeebeClient,
            ILogger<SendEmailJobWorker> logger,
            IProducer<string, string> kafkaProducer,
            IConfiguration configuration)
        {
            _zeebeClient = zeebeClient;
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _topic = configuration["Kafka:Topic"];
            _configuration = configuration;

        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Send Email job worker...");

            _sendEmailJobWorker = _zeebeClient.NewWorker()
                .JobType("send-email")
                .Handler(async (client, job) => await HandleKafkaJobAsync(client, job))
                .MaxJobsActive(5)
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            _logger.LogInformation("Send Email job worker started successfully");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Send Email job worker...");
            _sendEmailJobWorker?.Dispose();
            _logger.LogInformation("Send Email job worker stopped");
            return Task.CompletedTask;
        }

        private async Task HandleKafkaJobAsync(IJobClient client, IJob job)
        {
            try
            {
                _logger.LogInformation("Processing Kafka publish job {JobKey}", job.Key);

                // Extract variables from the job
                var variables = job.Variables;
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(variables);

                var applicationId = jsonElement.GetProperty("applicationId").GetString();
                var isApproved = jsonElement.GetProperty("IsApproved").GetString();

                var isCreated = (isApproved == "true" ? jsonElement.GetProperty("IsCreated").GetBoolean() : false);
                var to = jsonElement.GetProperty("Email").GetString();
                var coreBankingEmail = _configuration["CoreBankingEmail"]; // Inject IConfiguration

                // Determine subject and body based on approval status
                string subject, body, coreBankingEmailsubject, coreBankingEmailbody;

                if (isApproved.Equals("true") && isCreated)
                {
                    var accountId = jsonElement.GetProperty("AccountId").GetString();

                    subject = "Your Account Has Been Successfully Created!";
                    body = $"Dear Customer,\n\nWe are pleased to inform you that your account {accountId} has been approved and your account has been successfully created.\n\nThank you for choosing our bank.\n\nBest regards,\nBank Team";
                }
                else if (isApproved.Equals("true") && !isCreated)
                {
                    to = coreBankingEmail;
                    subject = $"Account Creation Failed for Application {applicationId}";
                    body = $"Account creation failed for approved application {applicationId} . Please investigate immediately.";
  
                }
                else 
                {
                    subject = "Update on Your Account Application";
                    body = $"Dear Customer,\n\nWe regret to inform you that your account {applicationId} could not be approved at this time.\n\nPlease contact our customer service for more information.\n\nBest regards,\nBank Team";
                }

                if (string.IsNullOrEmpty(applicationId))
                    throw new ArgumentException("ApplicationId is required");

                if (string.IsNullOrEmpty(to))
                    throw new ArgumentException("To is required");

                // Prepare Kafka message with email details
                var kafkaMessage = new
                {
                    ApplicationId = applicationId,
                    To = to,
                    Subject = subject,
                    Body = body,
                    Status = isApproved.Equals("true") ? "approved" : "rejected",
                    Timestamp = DateTime.UtcNow
                };

                var messageJson = JsonSerializer.Serialize(kafkaMessage);

                // Publish to Kafka
                var deliveryResult = await _kafkaProducer.ProduceAsync(
                    _topic,
                    new Message<string, string>
                    {
                        Key = applicationId,
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
    }
}
