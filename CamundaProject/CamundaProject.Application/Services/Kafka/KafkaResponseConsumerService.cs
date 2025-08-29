using CamundaProject.Core.Interfaces.Services.Email;
using CamundaProject.Core.Models.EmailModels;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Zeebe.Client;

namespace CamundaProject.Application.Services.Kafka
{
    public class KafkaResponseConsumerService : IHostedService
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly IConsumer<string, string> _kafkaConsumer;
        private readonly ILogger<KafkaResponseConsumerService> _logger;
        private Task? _consumingTask;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly string _responseTopic;

        public KafkaResponseConsumerService(
            IZeebeClient zeebeClient,
            ILogger<KafkaResponseConsumerService> logger,
            IConfiguration configuration)
        {
            _zeebeClient = zeebeClient;
            _logger = logger;
            _responseTopic = configuration["Kafka:ResponseTopic"];

            var config = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = configuration["Kafka:ConsumerGroupResponse"],
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _kafkaConsumer = new ConsumerBuilder<string, string>(config).Build();
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
            _kafkaConsumer.Subscribe(_responseTopic);

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

                        _logger.LogInformation("Received message from Kafka. Key: {Key}, Topic: {Topic}",
                            consumeResult.Message.Key, consumeResult.Topic);

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
                var responseData = JsonSerializer.Deserialize<JsonElement>(message.Value);
                var requestId = responseData.GetProperty("RequestId").GetString();
                var status = responseData.GetProperty("Status").GetString();

                if (string.IsNullOrEmpty(requestId))
                {
                    _logger.LogWarning("Invalid response message format - missing RequestId");
                    return;
                }

                if (status == "success")
                {
                    // Update the process with success response
                    await _zeebeClient.NewPublishMessageCommand()
                        .MessageName("ResponseMessage")
                        .CorrelationKey(requestId)
                        .Variables(JsonSerializer.Serialize(new
                        {
                            response = new
                            {
                                status = "success",
                                processedAt = DateTime.UtcNow
                            },
                            requestId = requestId
                        }))
                        .Send();

                    _logger.LogInformation("Success response sent for request ID: {RequestId}", requestId);
                }
                else
                {
                    // Update the process with error response
                    var errorMessage = responseData.GetProperty("ErrorMessage").GetString();

                    await _zeebeClient.NewPublishMessageCommand()
                        .MessageName("ResponseMessage")
                        .CorrelationKey(requestId)
                        .Variables(JsonSerializer.Serialize(new
                        {
                            error = new
                            {
                                message = errorMessage,
                                timestamp = DateTime.UtcNow
                            },
                            requestId = requestId
                        }))
                        .Send();

                    _logger.LogError("Error response sent for request ID: {RequestId}", requestId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing response message");
            }
        }
    }
}