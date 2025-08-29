using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
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

        public KafkaResponseConsumerService(
            IZeebeClient zeebeClient,
            IConsumer<string, string> kafkaConsumer,
            ILogger<KafkaResponseConsumerService> logger)
        {
            _zeebeClient = zeebeClient;
            _kafkaConsumer = kafkaConsumer;
            _logger = logger;
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
            _kafkaConsumer.Subscribe("test-topic"); // Match your response topic from BPMN

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
                // Deserialize the Kafka message
                var responseMessage = JsonSerializer.Deserialize<ResponseMessage>(message.Value);

                if (responseMessage == null || string.IsNullOrEmpty(responseMessage.RequestId))
                {
                    _logger.LogWarning("Invalid message format or missing requestId");
                    return;
                }

                _logger.LogInformation("Processing response for requestId: {RequestId}", responseMessage.RequestId);

                // Publish message to Zeebe to complete the intermediate catch event
                await _zeebeClient.NewPublishMessageCommand()
                    .MessageName("ResponseMessage") // Must match BPMN message name
                    .CorrelationKey(responseMessage.RequestId) // Must match the requestId
                    .Variables(JsonSerializer.Serialize(new
                    {
                        response = new
                        {
                            status = responseMessage.Status,
                            data = responseMessage.Data,
                            timestamp = responseMessage.Timestamp
                        },
                        requestId = responseMessage.RequestId
                    }))
                    .Send();

                _logger.LogInformation("Published message to Zeebe for requestId: {RequestId}", responseMessage.RequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Kafka message with key: {Key}", message.Key);
            }
        }
    }

    // Response message DTO
    public class ResponseMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "success" or "error"
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; }
    }
}