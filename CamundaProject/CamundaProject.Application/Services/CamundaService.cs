using CamundaProject.Application.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CamundaProject.Core.Interfaces.Services;
using CamundaProject.Core.Models;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;

namespace CamundaProject.Application.Services
{
    public class CamundaService : ICamundaService
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly ILogger<CamundaService> _logger;

        public CamundaService(IZeebeClient zeebeClient, ILogger<CamundaService> logger)
        {
            _zeebeClient = zeebeClient;
            _logger = logger;
        }

        public async Task<string> StartProcessInstanceAsync(string processDefinitionKey, Dictionary<string, object> variables)
        {
            try
            {
                _logger.LogInformation("Starting process instance for: {ProcessDefinitionKey}", processDefinitionKey);

                var processInstance = await _zeebeClient.NewCreateProcessInstanceCommand()
                    .BpmnProcessId(processDefinitionKey)
                    .LatestVersion()
                    .Variables(JsonConvert.SerializeObject(variables))
                    .Send();

                _logger.LogInformation("Started process instance: {ProcessInstanceKey}", processInstance.ProcessInstanceKey);
                return processInstance.ProcessInstanceKey.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting process instance for: {ProcessDefinitionKey}", processDefinitionKey);
                throw;
            }
        }

        public async Task DeployProcessDefinition(string resourcePath)
        {
            try
            {
                _logger.LogInformation("Deploying process definition from: {ResourcePath}", resourcePath);

                var deployment = await _zeebeClient.NewDeployCommand()
                    .AddResourceFile(resourcePath)
                    .Send();

                _logger.LogInformation("Deployed process definition successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying process definition from: {ResourcePath}", resourcePath);
                throw;
            }
        }

        public async Task CancelProcessInstanceAsync(string processInstanceKey)
        {
            try
            {
                _logger.LogInformation("Canceling process instance: {ProcessInstanceKey}", processInstanceKey);

                await _zeebeClient.NewCancelInstanceCommand(long.Parse(processInstanceKey))
                    .Send();

                _logger.LogInformation("Canceled process instance: {ProcessInstanceKey}", processInstanceKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling process instance: {ProcessInstanceKey}", processInstanceKey);
                throw;
            }
        }

        public async Task PublishMessageAsync(string messageName, string correlationKey, Dictionary<string, object> variables)
        {
            try
            {
                _logger.LogInformation("Publishing message: {MessageName} with correlation key: {CorrelationKey}", messageName, correlationKey);

                await _zeebeClient.NewPublishMessageCommand()
                    .MessageName(messageName)
                    .CorrelationKey(correlationKey)
                    .Variables(JsonConvert.SerializeObject(variables))
                    .Send();

                _logger.LogInformation("Published message: {MessageName}", messageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message: {MessageName}", messageName);
                throw;
            }
        }

        public async Task<string> GetTopologyAsync()
        {
            try
            {
                var topology = await _zeebeClient.TopologyRequest().Send();

                // In Zeebe 2.9.0, topology might not have the same structure
                // Return a simple string representation
                return $"Connected to Zeebe gateway. Brokers: {topology?.Brokers?.Count ?? 0}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting topology");
                throw;
            }
        }
    }
}