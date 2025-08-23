using CamundaProject.Application.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CamundaProject.Core.Interfaces.Services;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using CamundaProject.Core.Models.CamundaModels;

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

        //-------------------------------------------------------------------------------------
        // Process Definition Operations (Zeebe compatible)
        //-------------------------------------------------------------------------------------

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

                _logger.LogInformation("Deployed {Count} process definitions successfully", deployment.Processes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying process definition from: {ResourcePath}", resourcePath);
                throw;
            }
        }

        // Empty
        public async Task<List<ProcessDefinition>> GetProcessDefinitionsAsync()
        {
            try
            {
                // For older Zeebe versions, you need to use the Operate API or maintain your own deployment tracking
                // This is a fallback implementation since direct deployment querying isn't available

                _logger.LogWarning("NewDeploymentsQuery not available in this Zeebe client version. Using fallback implementation.");

                // Alternative: If you've been tracking deployments yourself, return that data
                // Otherwise, return empty list or implement Operate API integration

                return new List<ProcessDefinition>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting process definitions");
                throw;
            }
        }

        // Empty
        public async Task<ProcessDefinition> GetProcessDefinitionByKeyAsync(string key)
        {
            try
            {
                var processDefinitions = await GetProcessDefinitionsAsync();
                return processDefinitions.FirstOrDefault(pd => pd.BpmnProcessId == key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting process definition by key: {Key}", key);
                throw;
            }
        }

        // Empty
        public async Task<List<Deployment>> GetDeploymentsAsync()
        {
            try
            {
                // Fallback for older Zeebe versions
                _logger.LogWarning("NewDeploymentsQuery not available in this Zeebe client version. Using fallback implementation.");

                return new List<Deployment>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deployments");
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

        // Empty
        public async Task<List<ProcessInstance>> GetProcessInstancesAsync()
        {
            // IMPORTANT: Zeebe doesn't provide a direct API to query process instances
            // This would require Operate API integration or maintaining your own tracking
            _logger.LogWarning("GetProcessInstancesAsync is not directly supported by Zeebe client API");
            return new List<ProcessInstance>();
        }

        //-------------------------------------------------------------------------------------
        // Job Operations (Zeebe equivalent of tasks)
        //-------------------------------------------------------------------------------------

        // Empty
        public async Task<List<Job>> GetActiveJobsAsync(string jobType = null)
        {
            try
            {
                // Zeebe doesn't provide a direct API to query jobs
                // Jobs are typically handled by job workers that poll for available jobs
                _logger.LogWarning("GetActiveJobsAsync is not directly supported by Zeebe client API");
                return new List<Job>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active jobs");
                throw;
            }
        }

        // Empty
        public async Task CompleteJobAsync(string jobKey, Dictionary<string, object> variables)
        {
            try
            {
                // This would be called from within a job worker, not from the client
                _logger.LogWarning("CompleteJobAsync should be called from within a job worker implementation");

                // Example of how a job worker would complete a job:
                // await jobClient.NewCompleteJobCommand(job.Key)
                //     .Variables(JsonConvert.SerializeObject(variables))
                //     .Send();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing job: {JobKey}", jobKey);
                throw;
            }
        }

        // Empty
        public async Task FailJobAsync(string jobKey, string errorMessage)
        {
            try
            {
                // This would be called from within a job worker
                _logger.LogWarning("FailJobAsync should be called from within a job worker implementation");

                // Example: await jobClient.NewFailCommand(job.Key).Retries(job.Retries - 1).ErrorMessage(errorMessage).Send();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error failing job: {JobKey}", jobKey);
                throw;
            }
        }

        //-------------------------------------------------------------------------------------
        // Message Operations
        //-------------------------------------------------------------------------------------

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

        //-------------------------------------------------------------------------------------
        // Incident Operations (Limited in Zeebe without Operate)
        //-------------------------------------------------------------------------------------

        // Empty
        public async Task<List<Incident>> GetIncidentsAsync()
        {
            // Zeebe doesn't provide a direct API to query incidents
            // This requires Operate API integration
            _logger.LogWarning("GetIncidentsAsync is not directly supported by Zeebe client API");
            return new List<Incident>();
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