using CamundaProject.Core.Interfaces.Services;
using CamundaProject.Core.Models.RequestModels;
using GatewayProtocol;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CamundaProject.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {
        private readonly ICamundaService _camundaService;
        private readonly ILogger<JobsController> _logger;

        public JobsController(ICamundaService camundaService, ILogger<JobsController> logger)
        {
            _camundaService = camundaService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetJobs([FromQuery] long? processInstanceKey, [FromQuery] string jobType)
        {
            try
            {
                var jobs = _camundaService.GetActiveJobs(processInstanceKey, jobType);

                return Ok(new
                {
                    Success = true,
                    Count = jobs.Count,
                    Jobs = jobs,
                    Filters = new { processInstanceKey, jobType }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jobs");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [HttpGet("{jobKey}")]
        public IActionResult GetJob(long jobKey)
        {
            try
            {
                var jobs = _camundaService.GetActiveJobs();
                var job = jobs.Find(j => j.JobKey == jobKey);

                if (job == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Error = $"Job {jobKey} not found"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Job = job
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job: {JobKey}", jobKey);
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [HttpPost("{jobKey}/complete")]
        public async Task<IActionResult> CompleteJob(long jobKey, [FromBody] VariableRequest request)
        {
            try
            {
                _logger.LogInformation("Completing job: {JobKey}", jobKey);

                var result = await _camundaService.CompleteJobAsync(jobKey, request.Variables);

                if (result.Success)
                {
                    return Ok(new
                    {
                        Success = true,
                        JobKey = result.JobKey,
                        ProcessInstanceKey = result.ProcessInstanceKey,
                        Message = result.Message
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Success = false,
                        JobKey = result.JobKey,
                        Error = result.Message
                    });
                }
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

        [HttpPost("{jobKey}/fail")]
        public async Task<IActionResult> FailJob(long jobKey, [FromQuery] string errorMessage, [FromQuery] int retries = 3)
        {
            try
            {
                _logger.LogInformation("Failing job: {JobKey}", jobKey);

                var success = await _camundaService.FailJobAsync(jobKey, errorMessage, retries);

                if (success)
                {
                    return Ok(new
                    {
                        Success = true,
                        JobKey = jobKey,
                        Message = "Job failed successfully"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Success = false,
                        JobKey = jobKey,
                        Error = "Failed to mark job as failed"
                    });
                }
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
    }
}