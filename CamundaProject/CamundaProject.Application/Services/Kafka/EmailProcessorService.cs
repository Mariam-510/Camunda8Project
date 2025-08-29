// EmailProcessorService.cs
using CamundaProject.Core.Interfaces.Services.Email;
using CamundaProject.Core.Models.EmailModels;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CamundaProject.Application.Services.Kafka
{
    public class EmailProcessorService : IHostedService
    {
        private readonly IConsumer<string, string> _requestConsumer;
        private readonly IProducer<string, string> _responseProducer;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailProcessorService> _logger;
        private Task _consumingTask;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly string _requestTopic;
        private readonly string _responseTopic;

        public EmailProcessorService(
            IProducer<string, string> responseProducer,
            IEmailService emailService,
            ILogger<EmailProcessorService> logger,
            IConfiguration configuration)
        {
            _responseProducer = responseProducer;
            _emailService = emailService;
            _logger = logger;

            _requestTopic = configuration["Kafka:RequestTopic"];
            _responseTopic = configuration["Kafka:ResponseTopic"];

            var config = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = configuration["Kafka:ConsumerGroupRequest"],
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _requestConsumer = new ConsumerBuilder<string, string>(config).Build();
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Email Processor Service...");
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _consumingTask = Task.Run(ProcessEmailRequests, _cancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        private async Task ProcessEmailRequests()
        {
            _requestConsumer.Subscribe(_requestTopic);

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _requestConsumer.Consume(_cancellationTokenSource.Token);

                        if (consumeResult?.Message?.Value == null)
                            continue;

                        _logger.LogInformation("Processing email request. Key: {Key}", consumeResult.Message.Key);

                        // Process the email
                        await ProcessEmailMessage(consumeResult.Message);

                        _requestConsumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error consuming email request: {Error}", ex.Error.Reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error consuming email request");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Email processing cancelled");
            }
        }

        private async Task ProcessEmailMessage(Message<string, string> message)
        {
            try
            {
                var emailMessage = JsonSerializer.Deserialize<EmailMessage>(message.Value);

                if (emailMessage == null || string.IsNullOrEmpty(emailMessage.RequestId))
                {
                    _logger.LogWarning("Invalid email message format");
                    return;
                }

                // Send email
                EmailModel emailModel = new()
                {
                    To = emailMessage.To,
                    Subject = emailMessage.Subject,
                    Body = emailMessage.Body
                };

                bool success = await _emailService.SendEmailAsync(emailModel);

                // Prepare response
                object responseMessage;

                if (success)
                {
                    responseMessage = new
                    {
                        RequestId = emailMessage.RequestId,
                        Status = "success",
                        SentAt = DateTime.UtcNow,
                        Message = "Email sent successfully"
                    };
                }
                else
                {
                    responseMessage = new
                    {
                        RequestId = emailMessage.RequestId,
                        Status = "error",
                        ErrorMessage = "Failed to send email",
                        Timestamp = DateTime.UtcNow
                    };
                }

                var responseJson = JsonSerializer.Serialize(responseMessage);

                // Send response to response topic
                // Use the same correlation ID from headers if available
                string correlationId = emailMessage.RequestId;
                if (message.Headers != null)
                {
                    var correlationHeader = message.Headers.FirstOrDefault(h => h.Key == "correlationId");
                    if (correlationHeader != null)
                    {
                        correlationId = Encoding.UTF8.GetString(correlationHeader.GetValueBytes());
                    }
                }

                var response = new Message<string, string>
                {
                    Key = correlationId,
                    Value = responseJson
                };

                await _responseProducer.ProduceAsync(_responseTopic, response);
                _logger.LogInformation("Response sent for request ID: {RequestId}", emailMessage.RequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email message");

                // Send error response
                var errorResponse = new
                {
                    RequestId = message.Key,
                    Status = "error",
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                };

                var errorJson = JsonSerializer.Serialize(errorResponse);

                await _responseProducer.ProduceAsync(_responseTopic,
                    new Message<string, string> { Key = message.Key, Value = errorJson });
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Email Processor Service...");
            _cancellationTokenSource?.Cancel();

            if (_consumingTask != null)
            {
                await Task.WhenAny(_consumingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }

            _requestConsumer.Close();
            _requestConsumer.Dispose();
            _logger.LogInformation("Email Processor Service stopped");
        }
    
    }
}