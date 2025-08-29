using CamundaProject.Application.Mapping;
using CamundaProject.Application.Services.Camunda;
using CamundaProject.Application.Services.Email;
using CamundaProject.Application.Services.Kafka;
using CamundaProject.Application.Services.Zeebe;
using CamundaProject.Core.Interfaces.Services.Camounda;
using CamundaProject.Core.Interfaces.Services.Email;
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

            // Configure Zeebe Client for Camunda 8
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

            services.AddSingleton<IEmailService, EmailService>();

            // Add Kafka producer configuration
            services.AddSingleton<IProducer<string, string>>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();

                var config = new ProducerConfig
                {
                    BootstrapServers = configuration["Kafka:BootstrapServers"],
                    ClientId = configuration["Kafka:ClientId"],
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

            // Add Kafka Consumer Configuration
            services.AddSingleton<IConsumer<string, string>>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();

                var config = new ConsumerConfig
                {
                    BootstrapServers = configuration["Kafka:BootstrapServers"],
                    GroupId = configuration["Kafka:GroupId"],
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false,
                    EnableAutoOffsetStore = false
                };

                return new ConsumerBuilder<string, string>(config)
                    .SetErrorHandler((_, e) =>
                        sp.GetService<ILogger<IConsumer<string, string>>>()?
                            .LogError("Kafka consumer error: {Reason}", e.Reason))
                    .Build();
            });

            // Add hosted service for job workers
            services.AddHostedService<ZeebeJobWorkerService>();

            services.AddHostedService<KafkaJobWorkerService>();

            services.AddHostedService<KafkaResponseConsumerService>();

            return services;
        }
    }
}



