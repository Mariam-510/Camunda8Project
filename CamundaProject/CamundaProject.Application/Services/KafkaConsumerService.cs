using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CamundaProject.Application.Services
{
    public class KafkaConsumerService : IHostedService
    {
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly string _topic = "process-topic";
        private readonly IConsumer<Ignore, string> _consumer;
        private Task _executingTask;
        private CancellationTokenSource _cts;

        public KafkaConsumerService(ILogger<KafkaConsumerService> logger)
        {
            _logger = logger;

            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "camunda-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Kafka Consumer starting...");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _executingTask = Task.Run(() => ExecuteAsync(_cts.Token), _cts.Token);

            return Task.CompletedTask;
        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_topic);

            _logger.LogInformation("Kafka Consumer started, listening to topic: {Topic}", _topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = _consumer.Consume(stoppingToken);

                        if (result?.Message != null)
                        {
                            _logger.LogInformation("---------------------------");
                            _logger.LogInformation("📩 Received message: {Message}", result.Message.Value);
                            _logger.LogInformation("---------------------------");

                            // TODO: Replace log with EmailService.Send(result.Message.Value);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error while consuming Kafka message.");
                    }

                    await Task.Delay(500, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka Consumer shutting down...");
            }
            finally
            {
                _consumer.Close();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Kafka Consumer stopping...");

            if (_cts != null)
            {
                _cts.Cancel();
            }

            if (_executingTask != null)
            {
                await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));
            }

            _logger.LogInformation("Kafka Consumer stopped.");
        }
    }
}
