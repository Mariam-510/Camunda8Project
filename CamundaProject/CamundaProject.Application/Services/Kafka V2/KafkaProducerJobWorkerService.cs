using CamundaProject.Application.Services.Zeebe;
using CamundaProject.Core.Interfaces.Services;
using Confluent.Kafka;
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

namespace CamundaProject.Application.Services
{
    public class KafkaProducerJobWorkerService : IHostedService
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly ILogger<ZeebeJobWorkerService> _logger;
        private IJobWorker? _kafkaWorker;

        public KafkaProducerJobWorkerService(
            IZeebeClient zeebeClient,
            ILogger<ZeebeJobWorkerService> logger)
        {
            _zeebeClient = zeebeClient;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Kafka Producer job workers...");

            _kafkaWorker = _zeebeClient.NewWorker()
                .JobType("kafka-job")       // MUST match BPMN service task type
                .Handler(HandleKafkaJobAsync)
                .AutoCompletion()
                .MaxJobsActive(5)
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            _logger.LogInformation("Kafka Producer job workers started successfully");
            return Task.CompletedTask;
        }

        private async Task HandleKafkaJobAsync(IJobClient jobClient, IJob job)
        {
            try
            {
                // Deserialize job.Variables (JSON) into a Dictionary
                var vars = JsonSerializer.Deserialize<Dictionary<string, object>>(job.Variables)
                           ?? new Dictionary<string, object>();

                var kafkaBootstrap = Get(vars, "kafkaBootstrap", "localhost:9092");
                var kafkaTopic = Get(vars, "kafkaTopic", "email-events");

                if (!vars.TryGetValue("email", out var emailObj))
                    throw new InvalidOperationException("Process variable 'email' is required (object with to, subject, html).");

                // Serialize the 'email' object as message value
                var messageValue = emailObj is string s
                    ? s // already JSON string
                    : JsonSerializer.Serialize(emailObj);

                var config = new ProducerConfig
                {
                    BootstrapServers = kafkaBootstrap,
                    Acks = Acks.All,
                    LingerMs = 5,
                    EnableIdempotence = true
                };

                using var producer = new ProducerBuilder<string, string>(config).Build();

                // Use "to" field from email as key if available
                var key = ExtractEmailToOrDefault(emailObj, job.ProcessInstanceKey.ToString());

                _logger.LogInformation(
                    "[CamundaWorker] Producing to '{KafkaTopic}' -> key='{Key}' value={Value}",
                    kafkaTopic, key, messageValue);

                var result = await producer.ProduceAsync(
                    kafkaTopic,
                    new Message<string, string> { Key = key, Value = messageValue }
                );

                _logger.LogInformation(
                    "[CamundaWorker] Job {JobKey} completed for process {ProcessInstanceKey} (topic={Topic}, partition={Partition}, offset={Offset})",
                    job.Key, job.ProcessInstanceKey, result.Topic, result.Partition, result.Offset);

                await jobClient.NewCompleteJobCommand(job.Key)
                    .Variables(JsonSerializer.Serialize(new { status = "Kafka message sent", topic = kafkaTopic }))
                    .Send();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[CamundaWorker] ERROR while processing job {JobKey} for process {ProcessInstanceKey}",
                    job.Key, job.ProcessInstanceKey);

                await jobClient.NewFailCommand(job.Key)
                    .Retries(Math.Max(0, job.Retries - 1))
                    .ErrorMessage(ex.Message)
                    .Send();
            }
        }

        private static string Get(Dictionary<string, object> dict, string key, string fallback)
            => dict.TryGetValue(key, out var val) ? val?.ToString() ?? fallback : fallback;

        private static string ExtractEmailToOrDefault(object? emailObj, string fallback)
        {
            try
            {
                if (emailObj == null) return fallback;

                if (emailObj is string s)
                {
                    var doc = JsonDocument.Parse(s);
                    if (doc.RootElement.TryGetProperty("to", out var toEl))
                        return toEl.GetString() ?? fallback;
                }
                else if (emailObj is JsonElement el && el.ValueKind == JsonValueKind.Object)
                {
                    if (el.TryGetProperty("to", out var toEl))
                        return toEl.GetString() ?? fallback;
                }
            }
            catch
            {
                // ignored
            }
            return fallback;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _kafkaWorker?.Dispose();
            _logger.LogInformation("Zeebe job workers stopped");
            return Task.CompletedTask;
        }
   
    }
}