using CamundaProject.Application.Mapping;
using CamundaProject.Application.Services.Camunda;
using CamundaProject.Application.Services.Kafka;
using CamundaProject.Application.Services.Zeebe;
using CamundaProject.Core.Interfaces.Services;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeebe.Client;

namespace CamundaProject.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(typeof(AutoMapperProfile));

            // Configure Zeebe Client for Camunda 8 (Zeebe Client 2.9.0)
            services.AddSingleton(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var camundaConfig = configuration.GetSection("Camunda");
                var zeebeAddress = camundaConfig["ZeebeAddress"];
                var useTls = bool.Parse(camundaConfig["UseTLS"] ?? "false");

                // For Zeebe 2.9.0, the API is different - no Build() method
                if (useTls)
                {
                    return ZeebeClient.Builder()
                        .UseGatewayAddress(zeebeAddress)
                        .UseTransportEncryption()
                        .Build();
                }
                else
                {
                    return ZeebeClient.Builder()
                        .UseGatewayAddress(zeebeAddress)
                        .UsePlainText()
                        .Build();
                }
            });

            // Register services
            services.AddScoped<ICamundaService, CamundaService>();

            services.AddHttpClient<ICamundaRestService, CamundaRestService>(client =>
            {
                client.BaseAddress = new Uri(configuration["Camunda:BaseUrl"] ?? "http://localhost:8080");
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            services.AddScoped<ICamundaRestService, CamundaRestService>();

            // Add hosted service for job workers
            services.AddHostedService<ZeebeJobWorkerService>();

            //services.AddHostedService<KafkaProducerJobWorkerService>();

            //services.AddHostedService<KafkaConsumerService>();

            // Add Kafka producer configuration
            services.AddSingleton<IProducer<string, string>>(sp =>
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = "localhost:9092", // Your Kafka bootstrap servers
                    ClientId = "camunda-producer",
                    Acks = Acks.All,
                    MessageSendMaxRetries = 3,
                    RetryBackoffMs = 1000
                };

                return new ProducerBuilder<string, string>(config)
                    .SetErrorHandler((_, e) =>
                        sp.GetService<ILogger<IProducer<string, string>>>()?
                            .LogError("Kafka producer error: {Reason}", e.Reason))
                    .Build();
            });

            services.AddHostedService<KafkaJobWorkerService>();

            // Kafka Consumer Configuration
            services.AddSingleton<IConsumer<string, string>>(sp =>
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = "localhost:9092",
                    GroupId = "camunda-consumer-group", // Must match BPMN groupId
                    AutoOffsetReset = AutoOffsetReset.Earliest, // Must match BPMN setting
                    EnableAutoCommit = false,
                    EnableAutoOffsetStore = false
                };

                return new ConsumerBuilder<string, string>(config)
                    .SetErrorHandler((_, e) =>
                        sp.GetService<ILogger<IConsumer<string, string>>>()?
                            .LogError("Kafka consumer error: {Reason}", e.Reason))
                    .Build();
            });

            services.AddHostedService<KafkaResponseConsumerService>();

            return services;
        }
    }
}



