using CamundaProject.Core.Interfaces.Services;
using CamundaProject.Core.Models.CamundaModels;
using CamundaProject.Core.Models.RequestModels;
using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
using System.Text.Json;
using Zeebe.Client;

namespace CamundaProject.Application.Services
{
    public class CamundaService : ICamundaService
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly ILogger<CamundaService> _logger;
        private readonly IJobTrackingService _jobTrackingService;

        public CamundaService(IZeebeClient zeebeClient, IJobTrackingService jobTrackingService, ILogger<CamundaService> logger)
        {
            _zeebeClient = zeebeClient;
            _jobTrackingService = jobTrackingService;
            _logger = logger;
        }

        //-------------------------------------------------------------------------------------
        // Process Definition Operations (Zeebe compatible)
        //-------------------------------------------------------------------------------------

        public async Task<string> StartProcessInstanceAsync(string processDefinitionKey, VariableRequest variableRequest)
        {
            try
            {
                _logger.LogInformation("Starting process instance for: {ProcessDefinitionKey}", processDefinitionKey);

                var processInstance = await _zeebeClient.NewCreateProcessInstanceCommand()
                    .BpmnProcessId(processDefinitionKey)
                    .LatestVersion()
                    .Variables(JsonSerializer.Serialize(variableRequest.Variables))
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

        //-------------------------------------------------------------------------------------
        // Job Functions
        //-------------------------------------------------------------------------------------

        public async Task<JobCompletionResult> CompleteJobAsync(long jobKey, Dictionary<string, object> variables)
        {
            try
            {
                _logger.LogInformation("Completing job: {JobKey}", jobKey);

                await _zeebeClient.NewCompleteJobCommand(jobKey)
                    .Variables(JsonSerializer.Serialize(variables))
                    .Send();

                _jobTrackingService.UpdateJobStatus(jobKey, "Completed");
                _jobTrackingService.RemoveJob(jobKey);

                var job = _jobTrackingService.GetJob(jobKey) ?? new ActiveJob { ProcessInstanceKey = 0 };

                return new JobCompletionResult
                {
                    Success = true,
                    JobKey = jobKey,
                    ProcessInstanceKey = job.ProcessInstanceKey,
                    Message = "Job completed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing job: {JobKey}", jobKey);
                _jobTrackingService.UpdateJobStatus(jobKey, "Failed");

                return new JobCompletionResult
                {
                    Success = false,
                    JobKey = jobKey,
                    ProcessInstanceKey = 0,
                    Message = ex.Message
                };
            }
        }

        public async Task<bool> FailJobAsync(long jobKey, string errorMessage, int retries = 3)
        {
            try
            {
                _logger.LogInformation("Failing job: {JobKey}", jobKey);

                await _zeebeClient.NewFailCommand(jobKey)
                    .Retries(retries)
                    .ErrorMessage(errorMessage)
                    .Send();

                _jobTrackingService.UpdateJobStatus(jobKey, "Failed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error failing job: {JobKey}", jobKey);
                return false;
            }
        }

        public List<ActiveJob> GetActiveJobs(long? processInstanceKey = null, string jobType = null)
        {
            if (processInstanceKey.HasValue)
            {
                return _jobTrackingService.GetJobsByProcessInstance(processInstanceKey.Value);
            }

            if (!string.IsNullOrEmpty(jobType))
            {
                return _jobTrackingService.GetJobsByType(jobType);
            }

            return _jobTrackingService.GetAllJobs();
        }
    }
}