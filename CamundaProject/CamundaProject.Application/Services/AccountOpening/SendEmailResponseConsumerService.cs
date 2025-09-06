using CamundaProject.Core.Interfaces.Services.Email;
using CamundaProject.Core.Models.EmailModels;
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
using Zeebe.Client.Api.Worker;

namespace CamundaProject.Application.Services.AccountOpening
{
    public class SendEmailResponseConsumerService : IHostedService
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly IConsumer<string, string> _kafkaConsumer;
        private readonly ILogger<SendEmailResponseConsumerService> _logger;
        private readonly IEmailService _emailService;
        private Task? _consumingTask;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly string _topic;

        public SendEmailResponseConsumerService(
            IZeebeClient zeebeClient,
            IConsumer<string, string> kafkaConsumer,
            ILogger<SendEmailResponseConsumerService> logger,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _zeebeClient = zeebeClient;
            _kafkaConsumer = kafkaConsumer;
            _logger = logger;
            _emailService = emailService;
            _topic = configuration["Kafka:Topic"];
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Kafka response consumer...");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _consumingTask = Task.Run(ConsumeMessages, _cancellationTokenSource.Token);

            _logger.LogInformation("Kafka response consumer started");
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Kafka response consumer...");

            _cancellationTokenSource?.Cancel();

            if (_consumingTask != null)
            {
                await Task.WhenAny(_consumingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }

            _kafkaConsumer.Close();
            _kafkaConsumer.Dispose();

            _logger.LogInformation("Kafka response consumer stopped");
        }

        private async Task ConsumeMessages()
        {
            _kafkaConsumer.Subscribe(_topic); 

            try
            {
                while (!_cancellationTokenSource?.Token.IsCancellationRequested ?? false)
                {
                    try
                    {
                        var consumeResult = _kafkaConsumer.Consume(_cancellationTokenSource?.Token ?? default);

                        if (consumeResult?.Message?.Value == null)
                        {
                            continue;
                        }

                        _logger.LogInformation("Received message from Kafka. Key: {Key}, Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                            consumeResult.Message.Key, consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);

                        // Process the message and complete the Zeebe message event
                        await ProcessKafkaMessage(consumeResult.Message);

                        _kafkaConsumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error consuming Kafka message: {Error}", ex.Error.Reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error consuming Kafka message");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka consumption cancelled");
            }
        }

        private async Task ProcessKafkaMessage(Message<string, string> message)
        {
            try
            {
                var emailMessage = JsonSerializer.Deserialize<EmailMessage>(message.Value);

                if (emailMessage == null || string.IsNullOrEmpty(emailMessage.ApplicationId))
                {
                    _logger.LogWarning("Invalid email message format");
                    return;
                }

                EmailModel emailModel = new EmailModel()
                {
                    To = emailMessage.To,
                    Subject = emailMessage.Subject,
                    Body = emailMessage.Body
                };

                // Try sending email
                bool success = await _emailService.SendEmailAsync(emailModel);

                if (success)
                {
                    // ✅ success case
                    await _zeebeClient.NewPublishMessageCommand()
                        .MessageName("ResponseMessage")
                        .CorrelationKey(emailMessage.ApplicationId)
                        .Variables(JsonSerializer.Serialize(new
                        {
                            response = new
                            {
                                IsDelivered = true,
                                sentAt = DateTime.UtcNow,
                            },
                            //requestId = emailMessage.RequestId
                        }))
                        .Send();

                    _logger.LogInformation("Email sent for request ID: {ApplicationId}", emailMessage.ApplicationId);
                }
                else
                {
                    // ❌ failure case
                    await _zeebeClient.NewPublishMessageCommand()
                        .MessageName("ResponseMessage")
                        .CorrelationKey(emailMessage.ApplicationId)
                        .Variables(JsonSerializer.Serialize(new
                        {
                            response = new
                            {
                                IsDelivered = false,
                                errorMessage = "Failed to send email"
                            },
                            applicationId = emailMessage.ApplicationId  
                        }))
                        .Send();

                    _logger.LogWarning("Failed to send email for request ID: {ApplicationId}", emailMessage.ApplicationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email message");

                // ❌ exception case
                try
                {
                    var emailMessage = JsonSerializer.Deserialize<EmailMessage>(message.Value);
                    if (emailMessage != null && !string.IsNullOrEmpty(emailMessage.ApplicationId))  
                    {
                        await _zeebeClient.NewPublishMessageCommand()
                            .MessageName("ResponseMessage")
                            .CorrelationKey(emailMessage.ApplicationId)
                            .Variables(JsonSerializer.Serialize(new
                            {
                                response = new
                                {
                                    IsDelivered = false,
                                    errorMessage = ex.Message
                                },
                                applicationId = emailMessage.ApplicationId
                            }))
                            .Send();
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Error sending error response");
                }
            }
        }

    }
}
