using CamundaProject.Application.Services;
using CamundaProject.Core.Interfaces.Services;
using CamundaProject.Core.Models;
using CamundaProject.Core.Models.RequestModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CamundaProject.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessController : ControllerBase
    {
        private readonly ICamundaService _camundaService;
        private readonly ILogger<ProcessController> _logger;

        public ProcessController(ICamundaService camundaService, ILogger<ProcessController> logger)
        {
            _camundaService = camundaService;
            _logger = logger;
        }

        //-------------------------------------------------------------------------------------
        // Process Definition Endpoints
        //-------------------------------------------------------------------------------------

        [HttpPost("start/{processDefinitionKey}")]
        public async Task<IActionResult> StartProcessInstance(
            string processDefinitionKey,
            [FromBody] Dictionary<string, object> variables)
        {
            try
            {
                _logger.LogInformation("Starting process instance for: {ProcessDefinitionKey}", processDefinitionKey);

                var processInstanceKey = await _camundaService.StartProcessInstanceAsync(
                    processDefinitionKey, variables);

                return Ok(new
                {
                    Success = true,
                    ProcessInstanceKey = processInstanceKey,
                    ProcessDefinitionKey = processDefinitionKey,
                    Message = "Process instance started successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting process instance for: {ProcessDefinitionKey}", processDefinitionKey);
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    ProcessDefinitionKey = processDefinitionKey
                });
            }
        }

        [HttpPost("deploy")]
        public async Task<IActionResult> DeployProcessDefinition([FromBody] DeployProcessRequest request)
        {
            try
            {
                _logger.LogInformation("Deploying process definition from: {ResourcePath}", request.ResourcePath);

                await _camundaService.DeployProcessDefinition(request.ResourcePath);

                return Ok(new
                {
                    Success = true,
                    Message = "Process definition deployed successfully",
                    ResourcePath = request.ResourcePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying process definition from: {ResourcePath}", request.ResourcePath);
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    ResourcePath = request.ResourcePath
                });
            }
        }

        // Empty
        [HttpGet("definitions")]
        public async Task<IActionResult> GetProcessDefinitions()
        {
            try
            {
                var processDefinitions = await _camundaService.GetProcessDefinitionsAsync();

                return Ok(new
                {
                    Success = true,
                    Count = processDefinitions.Count,
                    ProcessDefinitions = processDefinitions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting process definitions");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        // Empty
        [HttpGet("definitions/{key}")]
        public async Task<IActionResult> GetProcessDefinitionByKey(string key)
        {
            try
            {
                var processDefinition = await _camundaService.GetProcessDefinitionByKeyAsync(key);

                if (processDefinition == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Error = $"Process definition with key '{key}' not found"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    ProcessDefinition = processDefinition
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting process definition by key: {Key}", key);
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    Key = key
                });
            }
        }

        // Empty
        [HttpGet("deployments")]
        public async Task<IActionResult> GetDeployments()
        {
            try
            {
                var deployments = await _camundaService.GetDeploymentsAsync();

                return Ok(new
                {
                    Success = true,
                    Count = deployments.Count,
                    Deployments = deployments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deployments");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        //-------------------------------------------------------------------------------------
        // Process Instance Endpoints
        //-------------------------------------------------------------------------------------

        [HttpDelete("instances/{processInstanceKey}")]
        public async Task<IActionResult> CancelProcessInstance(string processInstanceKey)
        {
            try
            {
                _logger.LogInformation("Canceling process instance: {ProcessInstanceKey}", processInstanceKey);

                await _camundaService.CancelProcessInstanceAsync(processInstanceKey);

                return Ok(new
                {
                    Success = true,
                    Message = "Process instance canceled successfully",
                    ProcessInstanceKey = processInstanceKey
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling process instance: {ProcessInstanceKey}", processInstanceKey);
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    ProcessInstanceKey = processInstanceKey
                });
            }
        }

        // Empty
        [HttpGet("instances")]
        public async Task<IActionResult> GetProcessInstances()
        {
            try
            {
                var processInstances = await _camundaService.GetProcessInstancesAsync();

                return Ok(new
                {
                    Success = true,
                    Count = processInstances.Count,
                    ProcessInstances = processInstances,
                    Message = "Note: Process instance querying is limited in Zeebe without Operate API integration"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting process instances");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        //-------------------------------------------------------------------------------------
        // Job Endpoints
        //-------------------------------------------------------------------------------------

        // Empty
        [HttpGet("jobs")]
        public async Task<IActionResult> GetActiveJobs([FromQuery] string jobType = null)
        {
            try
            {
                var jobs = await _camundaService.GetActiveJobsAsync(jobType);

                return Ok(new
                {
                    Success = true,
                    Count = jobs.Count,
                    Jobs = jobs,
                    Message = "Note: Job querying is limited in Zeebe - jobs are typically handled by job workers"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active jobs");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        // Empty
        [HttpPost("jobs/{jobKey}/complete")]
        public async Task<IActionResult> CompleteJob(string jobKey, [FromBody] CompleteJobRequest request)
        {
            try
            {
                _logger.LogInformation("Completing job: {JobKey}", jobKey);

                await _camundaService.CompleteJobAsync(jobKey, request.Variables);

                return Ok(new
                {
                    Success = true,
                    Message = "Job completed successfully",
                    JobKey = jobKey
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing job: {JobKey}", jobKey);
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    JobKey = jobKey
                });
            }
        }

        // Empty
        [HttpPost("jobs/{jobKey}/fail")]
        public async Task<IActionResult> FailJob(string jobKey, [FromBody] FailJobRequest request)
        {
            try
            {
                _logger.LogInformation("Failing job: {JobKey}", jobKey);

                await _camundaService.FailJobAsync(jobKey, request.ErrorMessage);

                return Ok(new
                {
                    Success = true,
                    Message = "Job failed successfully",
                    JobKey = jobKey
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error failing job: {JobKey}", jobKey);
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    JobKey = jobKey
                });
            }
        }

        //-------------------------------------------------------------------------------------
        // Message Endpoints
        //-------------------------------------------------------------------------------------

        [HttpPost("messages/publish")]
        public async Task<IActionResult> PublishMessage([FromBody] PublishMessageRequest request)
        {
            try
            {
                _logger.LogInformation("Publishing message: {MessageName}", request.MessageName);

                await _camundaService.PublishMessageAsync(
                    request.MessageName, request.CorrelationKey, request.Variables);

                return Ok(new
                {
                    Success = true,
                    Message = "Message published successfully",
                    MessageName = request.MessageName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message: {MessageName}", request.MessageName);
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    MessageName = request.MessageName
                });
            }
        }

        //-------------------------------------------------------------------------------------
        // Incident Endpoints
        //-------------------------------------------------------------------------------------

        // Empty
        [HttpGet("incidents")]
        public async Task<IActionResult> GetIncidents()
        {
            try
            {
                var incidents = await _camundaService.GetIncidentsAsync();

                return Ok(new
                {
                    Success = true,
                    Count = incidents.Count,
                    Incidents = incidents,
                    Message = "Note: Incident querying requires Operate API integration in Zeebe"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incidents");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }
       
        //-------------------------------------------------------------------------------------
        // Utility Endpoints
        //-------------------------------------------------------------------------------------

        [HttpGet("topology")]
        public async Task<IActionResult> GetTopology()
        {
            try
            {
                var topologyMessage = await _camundaService.GetTopologyAsync();

                return Ok(new
                {
                    Success = true,
                    Message = topologyMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting topology");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var isConnected = await _camundaService.TestConnectionAsync();

                return Ok(new
                {
                    Connected = isConnected,
                    Message = isConnected ? "Connection successful" : "Connection failed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Connected = false,
                    Error = ex.Message
                });
            }
        }
    }

}