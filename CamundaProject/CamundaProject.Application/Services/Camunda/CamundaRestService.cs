using CamundaProject.Core.Interfaces.Services.Camounda;
using CamundaProject.Core.Models.RestRequestModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CamundaProject.Application.Services.Camunda
{
    public class CamundaRestService : ICamundaRestService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CamundaRestService> _logger;
        private readonly string _baseUrl;

        public CamundaRestService(
            HttpClient httpClient,
            ILogger<CamundaRestService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["Camunda:BaseUrl"] ?? "http://localhost:8080";
        }

        public async Task<object> StartProcessInstanceAsync(StartProcessRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate that either processDefinitionKey or processDefinitionId is provided
            if (string.IsNullOrEmpty(request.ProcessDefinitionKey) && string.IsNullOrEmpty(request.ProcessDefinitionId))
                throw new ArgumentException("Either ProcessDefinitionKey or ProcessDefinitionId must be provided");

            if (!string.IsNullOrEmpty(request.ProcessDefinitionKey) && !string.IsNullOrEmpty(request.ProcessDefinitionId))
                throw new ArgumentException("Either ProcessDefinitionKey or ProcessDefinitionId must be provided");

            var endpoint = $"{_baseUrl}/v2/process-instances";

            try
            {
                _logger.LogInformation("Starting process instance. ProcessDefinitionKey: {Key}, ProcessDefinitionId: {Id}",
                    request.ProcessDefinitionKey, request.ProcessDefinitionId);

                // Prepare the request payload
                var payload = new
                {
                    processDefinitionKey = request.ProcessDefinitionKey,
                    processDefinitionId = request.ProcessDefinitionId,
                    version = request.Version,
                    variables = request.VariableRequest.Variables
                };

                var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to start process instance. Status: {StatusCode}, Response: {Error}",
                        response.StatusCode, errorContent);

                    response.EnsureSuccessStatusCode(); // This will throw for non-success status codes
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully started process instance. Response: {Response}", responseContent);
               
                var result = JsonSerializer.Deserialize<object>(responseContent);

                return result ?? new object();

            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while starting process instance");
                throw new Exception($"Failed to start process instance: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while starting process instance");
                throw new Exception($"An error occurred while starting process instance: {ex.Message}", ex);
            }
        }

        public async Task<object> SearchUserTasksAsync(UserTaskSearchRequest request)
        {
            var endpoint = $"{_baseUrl}/v2/user-tasks/search";

            try
            {
                _logger.LogInformation("Starting search user tasks.");

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false
                });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to search user tasks. Status: {StatusCode}, Response: {Error}",
                        response.StatusCode, errorContent);
                    response.EnsureSuccessStatusCode();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully search user tasks. Response: {Response}", responseContent);

                var result = JsonSerializer.Deserialize<object>(responseContent);

                return result ?? new object();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching user tasks");
                throw;
            }
        }

        public async Task<object> CompleteUserTaskAsync(string userTaskKey, CompleteUserTaskRequest request)
        {
            var endpoint = $"{_baseUrl}/v2/user-tasks/{userTaskKey}/completion";

            try
            {
                _logger.LogInformation("Completing user task. Key: {UserTaskKey}", userTaskKey);

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to complete user task. Status: {StatusCode}, Response: {Error}",
                        response.StatusCode, errorContent);
                    response.EnsureSuccessStatusCode();
                }

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    _logger.LogInformation("Successfully completed user task {UserTaskKey}.", userTaskKey);
                    return new { success = true, message = "Task completed successfully" };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response after completing user task: {Response}", responseContent);

                return string.IsNullOrWhiteSpace(responseContent)
                    ? new { success = true }
                    : JsonSerializer.Deserialize<object>(responseContent) ?? new { success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing user task {UserTaskKey}", userTaskKey);
                throw;
            }
        }

    }
}
