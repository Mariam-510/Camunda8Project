using CamundaProject.Core.Interfaces.Services;
using CamundaProject.Core.Models.RestRequestModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CamundaProject.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class RestProcessController : ControllerBase
    {
        private readonly ICamundaRestService _camundaService;
        private readonly ILogger<RestProcessController> _logger;

        public RestProcessController(ICamundaRestService camundaService, ILogger<RestProcessController> logger)
        {
            _camundaService = camundaService;
            _logger = logger;
        }

        [HttpPost("start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartProcessInstance([FromBody] StartProcessRequest request)
        {
            try
            {
                _logger.LogInformation("Starting process instance. ProcessDefinitionKey: {Key}, ProcessDefinitionId: {Id}",
                    request?.ProcessDefinitionKey, request?.ProcessDefinitionId);

                if (request == null)
                {
                    return BadRequest("Request body cannot be null");
                }

                var result = await _camundaService.StartProcessInstanceAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request parameters");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting process instance");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error starting process: {ex.Message}");
            }
        }

        [HttpPost("start/by-key/{processDefinitionKey}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartProcessByKey(
            [FromRoute] string processDefinitionKey,
            [FromBody] Dictionary<string, object>? variables = null)
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
                    Variables = variables ?? new Dictionary<string, object>()
                };

                var result = await _camundaService.StartProcessInstanceAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting process instance by key: {Key}", processDefinitionKey);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error starting process: {ex.Message}");
            }
        }


        [HttpPost("start/by-id/{processDefinitionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartProcessById(
            [FromRoute] string processDefinitionId,
            [FromQuery] int? version = null,
            [FromBody] Dictionary<string, object>? variables = null)
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
                    Variables = variables ?? new Dictionary<string, object>()
                };

                var result = await _camundaService.StartProcessInstanceAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting process instance by ID: {Id}", processDefinitionId);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error starting process: {ex.Message}");
            }
        }

    }
}