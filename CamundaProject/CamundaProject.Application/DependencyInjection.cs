using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamundaProject.Application.Mapping;
using CamundaProject.Application.Services;
using CamundaProject.Core.Interfaces.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zeebe.Client;

namespace CamundaProject.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(typeof(AutoMapperProfile));

            // Update service registration to use interface
            services.AddScoped<ICamundaService, CamundaService>();

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


            return services;
        }
    }
}



