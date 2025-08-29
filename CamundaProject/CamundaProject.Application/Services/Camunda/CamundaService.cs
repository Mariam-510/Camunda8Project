using CamundaProject.Core.Interfaces.Services.Camounda;
using CamundaProject.Core.Models.RequestModels;
using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
using System.Text.Json;
using Zeebe.Client;

namespace CamundaProject.Application.Services.Camunda
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

        //-------------------------------------------------------------------------------------
        // Process Definition Operations (Zeebe compatible)
        //-------------------------------------------------------------------------------------

        public async Task<string> StartProcessInstanceAsync(string processDefinitionId, VariableRequest variableRequest)
        {
            try
            {
                _logger.LogInformation("Starting process instance for: {processDefinitionId}", processDefinitionId);

                var processInstance = await _zeebeClient.NewCreateProcessInstanceCommand()
                    .BpmnProcessId(processDefinitionId)
                    .LatestVersion()
                    .Variables(JsonSerializer.Serialize(variableRequest.Variables))
                    .Send();

                _logger.LogInformation("Started process instance: {ProcessInstanceKey}", processInstance.ProcessInstanceKey);

                return processInstance.ProcessInstanceKey.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting process instance for: {processDefinitionId}", processDefinitionId);
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

                _logger.LogInformation("Deployed {Count} process definitions successfully", deployment.Processes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying process definition from: {ResourcePath}", resourcePath);
                throw;
            }
        }

        //-------------------------------------------------------------------------------------
        // Process Instance Operations (Zeebe compatible)
        //-------------------------------------------------------------------------------------

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

        //-------------------------------------------------------------------------------------
        // Message Operations
        //-------------------------------------------------------------------------------------

        public async Task PublishMessageAsync(string messageName, string correlationKey, VariableRequest variableRequest)
        {
            try
            {
                _logger.LogInformation("Publishing message: {MessageName} with correlation key: {CorrelationKey}", messageName, correlationKey);

                await _zeebeClient.NewPublishMessageCommand()
                    .MessageName(messageName)
                    .CorrelationKey(correlationKey)
                    .Variables(JsonSerializer.Serialize(variableRequest.Variables))
                    .Send();

                _logger.LogInformation("Published message: {MessageName}", messageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message: {MessageName}", messageName);
                throw;
            }
        }

        //-------------------------------------------------------------------------------------
        // Utility Operations
        //-------------------------------------------------------------------------------------

        public async Task<string> GetTopologyAsync()
        {
            try
            {
                var topology = await _zeebeClient.TopologyRequest().Send();
                return $"Connected to Zeebe gateway. Brokers: {topology?.Brokers?.Count ?? 0}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting topology");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await _zeebeClient.TopologyRequest().Send();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}