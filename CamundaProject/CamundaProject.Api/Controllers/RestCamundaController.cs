using CamundaProject.Core.Interfaces.Services.Camounda;
using CamundaProject.Core.Models.RequestModels;
using CamundaProject.Core.Models.RestRequestModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CamundaProject.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class RestCamundaController : ControllerBase
    {
        private readonly ICamundaRestService _camundaRestService;
        private readonly ILogger<RestCamundaController> _logger;

        public RestCamundaController(ICamundaRestService camundaRestService, ILogger<RestCamundaController> logger)
        {
            _camundaRestService = camundaRestService;
            _logger = logger;
        }

        [HttpPost("start/by-key/{processDefinitionKey}")]
        public async Task<IActionResult> StartProcessByKey(
            [FromRoute] string processDefinitionKey,
            [FromBody] VariableRequest variableRequest)
        {
            try
            {
                _logger.LogInformation("Starting process instance by key: {Key}", processDefinitionKey);

                if (string.IsNullOrEmpty(processDefinitionKey))
                {
                    return BadRequest("Process definition key is required");
                }

                var request = new StartProcessRequest
                {
                    ProcessDefinitionKey = processDefinitionKey,
                    VariableRequest = variableRequest
                };

                var result = await _camundaRestService.StartProcessInstanceAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting process instance by key: {Key}", processDefinitionKey);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error starting process: {ex.Message}");
            }
        }


        [HttpPost("start/by-id/{processDefinitionId}")]
        public async Task<IActionResult> StartProcessById(
            [FromRoute] string processDefinitionId,
            [FromBody] VariableRequest variableRequest,
            [FromQuery] int? version = null)
        {
            try
            {
                _logger.LogInformation("Starting process instance by ID: {Id}, Version: {Version}",
                    processDefinitionId, version);

                if (string.IsNullOrEmpty(processDefinitionId))
                {
                    return BadRequest("Process definition ID is required");
                }

                var request = new StartProcessRequest
                {
                    ProcessDefinitionId = processDefinitionId,
                    Version = version,
                    VariableRequest = variableRequest
                };

                var result = await _camundaRestService.StartProcessInstanceAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting process instance by ID: {Id}", processDefinitionId);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error starting process: {ex.Message}");
            }
        }

        [HttpPost("user-tasks/search")]
        public async Task<IActionResult> SearchUserTasks([FromBody] UserTaskSearchRequest request)
        {
            try
            {
                var result = await _camundaRestService.SearchUserTasksAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching user tasks");
                return StatusCode(500, $"Error searching user tasks: {ex.Message}");
            }
        }

        [HttpPost("user-tasks/{userTaskKey}/complete")]
        public async Task<IActionResult> CompleteUserTask(
            [FromRoute] string userTaskKey,
            [FromBody] CompleteUserTaskRequest request)
        {
            try
            {
                var result = await _camundaRestService.CompleteUserTaskAsync(userTaskKey, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing user task {UserTaskKey}", userTaskKey);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error completing user task: {ex.Message}");
            }
        }

    }
}