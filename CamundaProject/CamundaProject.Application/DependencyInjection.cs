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

            // Add Kafka producers
            services.AddSingleton<IProducer<string, string>>(provider =>
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = configuration["Kafka:BootstrapServers"]
                };
                return new ProducerBuilder<string, string>(config).Build();
            });

            // Add hosted services
            services.AddHostedService<KafkaJobWorkerService>();
            services.AddHostedService<EmailProcessorService>();
            services.AddHostedService<KafkaResponseConsumerService>();

            return services;
        }
    }
}



