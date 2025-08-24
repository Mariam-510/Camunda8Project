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
            [FromBody] VariableRequest variableRequest)
        {
            try
            {
                _logger.LogInformation("Starting process instance for: {ProcessDefinitionKey}", processDefinitionKey);

                var processInstanceKey = await _camundaService.StartProcessInstanceAsync(
                    processDefinitionKey, variableRequest);

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
                    request.MessageName, request.CorrelationKey, request.variableRequest);

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