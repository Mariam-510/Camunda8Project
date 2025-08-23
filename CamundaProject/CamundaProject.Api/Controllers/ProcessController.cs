using CamundaProject.Application.Services;
using CamundaProject.Core.Interfaces.Services;
using CamundaProject.Core.Models;
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

        [HttpPost("publish-message")]
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

        [HttpDelete("cancel/{processInstanceKey}")]
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

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var topologyMessage = await _camundaService.GetTopologyAsync();

                return Ok(new
                {
                    Connected = true,
                    Message = topologyMessage
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