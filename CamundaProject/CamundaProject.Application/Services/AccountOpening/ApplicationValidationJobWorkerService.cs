using CamundaProject.Core.Models.ClientModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace CamundaProject.Application.Services.AccountOpening
{
    public class ApplicationValidationJobWorkerService : IHostedService
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly ILogger<ApplicationValidationJobWorkerService> _logger;
        private readonly IConfiguration _configuration;
        private IJobWorker? _applicationValidationWorker;

        public ApplicationValidationJobWorkerService(
            IZeebeClient zeebeClient,
            ILogger<ApplicationValidationJobWorkerService> logger,
            IConfiguration configuration)
        {
            _zeebeClient = zeebeClient;
            _logger = logger;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Create a Zeebe job worker for application validation
            _applicationValidationWorker = _zeebeClient.NewWorker()
                .JobType("application-validation")
                .Handler(HandleValidationJob)
                .MaxJobsActive(5)
                .Name("application-validation-worker")
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromMinutes(5)) // Longer timeout for validation
                .Open();

            _logger.LogInformation("Application Validation Job Worker started");
            return Task.CompletedTask;
        }

        private async void HandleValidationJob(IJobClient client, IJob job)
        {
            _logger.LogInformation($"Handling validation job {job.Key}");

            try
            {
                // Parse variables from the job
                var variables = JsonSerializer.Deserialize<JsonElement>(job.Variables);

                // Extract application data
                var applicationData = new ApplicationForm
                {
                    ApplicationId = variables.GetProperty("applicationId").GetString() ?? "",
                    FullName = variables.GetProperty("FullName").GetString() ?? "",
                    NationalId = variables.GetProperty("NationalId").GetString() ?? "",
                    ClientAddress = variables.GetProperty("ClientAddress").GetString() ?? "",
                    DateOfBirth = DateTime.Parse(variables.GetProperty("DateOfBirth").GetString() ?? ""),
                    DepositAmount = variables.GetProperty("DepositAmount").GetDecimal(),
                    //NationalIdImage = variables.GetProperty("NationalIdImage")[0].ToString() ?? ""
                    NationalIdImage = variables.GetProperty("NationalIdImage")[0].GetRawText()
                };

                // Perform comprehensive validation
                var validationResult = ValidateApplication(applicationData);

                // Complete the job with validation results
                await client.NewCompleteJobCommand(job.Key)
                    .Variables(JsonSerializer.Serialize(new
                    {
                        IsValid = validationResult.IsValid,
                        validationResult.AttributeResults
                    }))
                    .Send();

                _logger.LogInformation($"Validation completed for application {applicationData.ApplicationId}. " +
                                      $"IsValid: {validationResult.IsValid}");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to handle validation job {job.Key}");

                // Fail the job to allow retries
                await client.NewFailCommand(job.Key)
                    .Retries(job.Retries - 1)
                    .ErrorMessage($"Validation failed: {ex.Message}")
                    .Send();
            }
        }

        private EnhancedValidationResult ValidateApplication(ApplicationForm application)
        {
            var result = new EnhancedValidationResult();
            var now = DateTime.Now;

            // Validate FullName
            result.FullNameResult = ValidateFullName(application.FullName);

            // Validate NationalId
            result.NationalIdResult = ValidateNationalId(application.NationalId);

            // Validate ClientAddress
            result.ClientAddressResult = ValidateClientAddress(application.ClientAddress);

            // Validate DateOfBirth
            result.DateOfBirthResult = ValidateDateOfBirth(application.DateOfBirth);

            // Validate DepositAmount
            result.DepositAmountResult = ValidateDepositAmount(application.DepositAmount);

            // Validate NationalIdImage
            result.NationalIdImageResult = ValidateNationalIdImage(application.NationalIdImage);

            // Check if all validations passed
            result.IsValid = result.FullNameResult.IsValid &&
                            result.NationalIdResult.IsValid &&
                            result.ClientAddressResult.IsValid &&
                            result.DateOfBirthResult.IsValid &&
                            result.DepositAmountResult.IsValid &&
                            result.NationalIdImageResult.IsValid;

            return result;
        }

        private AttributeValidationResult ValidateFullName(string fullName)
        {
            var result = new AttributeValidationResult { AttributeName = "FullName" };

            if (string.IsNullOrWhiteSpace(fullName))
            {
                result.AddError("Full name is required");
                return result;
            }

            if (fullName.Length < 2)
            {
                result.AddError("Full name must be at least 2 characters long");
            }

            if (fullName.Length > 100)
            {
                result.AddError("Full name cannot exceed 100 characters");
            }

            if (!Regex.IsMatch(fullName, @"^[\p{L} \.\-']+$"))
            {
                result.AddError("Full name contains invalid characters");
            }

            // Check for suspicious patterns (e.g., multiple capital letters in middle of name)
            var nameParts = fullName.Split(' ');
            if (nameParts.Length < 2)
            {
                result.AddError("Full name should typically include first and last name");
            }

            result.IsValid = result.Errors.Length == 0;
            return result;
        }

        private AttributeValidationResult ValidateNationalId(string nationalId)
        {
            var result = new AttributeValidationResult { AttributeName = "NationalId" };

            if (string.IsNullOrWhiteSpace(nationalId))
            {
                result.AddError("National ID is required");
                return result;
            }

            // Remove any non-alphanumeric characters
            var cleanNationalId = Regex.Replace(nationalId, @"[^a-zA-Z0-9]", "");

            if (cleanNationalId.Length != 14)
            {
                result.AddError("National ID should be 14 char");
            }

            if (!cleanNationalId.All(char.IsLetterOrDigit))
            {
                result.AddError("National ID contains invalid characters");
            }

            // Check for common fake patterns
            if (cleanNationalId.Distinct().Count() == 1)
            {
                result.AddError("National ID appears to be a repeated pattern");
            }

            result.IsValid = result.Errors.Length == 0;
            return result;
        }

        private AttributeValidationResult ValidateClientAddress(string address)
        {
            var result = new AttributeValidationResult { AttributeName = "ClientAddress" };

            if (string.IsNullOrWhiteSpace(address))
            {
                result.AddError("Address is required");
                return result;
            }

            if (address.Length < 10)
            {
                result.AddError("Address appears to be incomplete");
            }

            if (address.Length > 200)
            {
                result.AddError("Address is too long");
            }

            result.IsValid = result.Errors.Length == 0;
            return result;
        }

        private AttributeValidationResult ValidateDateOfBirth(DateTime dateOfBirth)
        {
            var result = new AttributeValidationResult { AttributeName = "DateOfBirth" };
            var now = DateTime.Now;
            var age = now.Year - dateOfBirth.Year - (now.DayOfYear < dateOfBirth.DayOfYear ? 1 : 0);

            if (age < 18)
            {
                result.AddError("Applicant must be at least 18 years old");
            }

            //if (dateOfBirth > now)
            //{
            //    result.AddError("Date of birth cannot be in the future");
            //    return result;
            //}

            if (dateOfBirth < now.AddYears(-120))
            {
                result.AddError("Date of birth appears to be invalid");
                return result;
            }

            result.IsValid = result.Errors.Length == 0;
            return result;
        }

        private AttributeValidationResult ValidateDepositAmount(decimal depositAmount)
        {
            var result = new AttributeValidationResult { AttributeName = "DepositAmount" };
            var minDeposit = 3000;
            //var minDeposit = _configuration.GetValue<decimal>("ValidationRules:MinDepositAmount", 3000);

            if (depositAmount <= 0)
            {
                result.AddError("Deposit amount must be greater than 0");
                return result;
            }

            if (depositAmount < minDeposit)
            {
                result.AddError($"Minimum deposit amount is {minDeposit:C}");
            }

            result.IsValid = result.Errors.Length == 0;
            return result;
        }

        private AttributeValidationResult ValidateNationalIdImage(string nationalIdImageJson)
        {
            var result = new AttributeValidationResult { AttributeName = "NationalIdImage" };

            if (string.IsNullOrWhiteSpace(nationalIdImageJson))
            {
                result.AddError("National ID image is required");
                return result;
            }

            try
            {
                var imageElement = JsonSerializer.Deserialize<JsonElement>(nationalIdImageJson);

                // Ensure metadata exists
                if (!imageElement.TryGetProperty("metadata", out var metadata))
                {
                    result.AddError("Image metadata is missing");
                    return result;
                }

                // Check content type
                if (metadata.TryGetProperty("contentType", out var contentTypeProp))
                {
                    var contentType = contentTypeProp.GetString();

                    if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/"))
                    {
                        result.AddError("Uploaded file is not a valid image");
                    }
                }
                else
                {
                    result.AddError("Content type is missing in metadata");
                }

                //// Optional: check file extension
                //if (metadata.TryGetProperty("fileName", out var fileNameProp))
                //{
                //    var fileName = fileNameProp.GetString();
                //    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                //    if (!allowedExtensions.Any(ext => fileName?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true))
                //    {
                //        result.AddError("File extension is unusual for an image");
                //    }
                //}
            }
            catch (Exception ex)
            {
                result.AddError($"Invalid image format: {ex.Message}");
            }

            result.IsValid = result.Errors.Length == 0;
            return result;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _applicationValidationWorker?.Dispose();
            _logger.LogInformation("Application Validation Job Worker stopped");
            return Task.CompletedTask;
        }
    }


 

    public class EnhancedValidationResult
    {
        public bool IsValid { get; set; }
        public AttributeValidationResult FullNameResult { get; set; }
        public AttributeValidationResult NationalIdResult { get; set; }
        public AttributeValidationResult ClientAddressResult { get; set; }
        public AttributeValidationResult DateOfBirthResult { get; set; }
        public AttributeValidationResult DepositAmountResult { get; set; }
        public AttributeValidationResult NationalIdImageResult { get; set; }

        // Helper property to get all attribute results
        public Dictionary<string, AttributeValidationResult> AttributeResults => new()
        {
            { "FullName", FullNameResult },
            { "NationalId", NationalIdResult },
            { "ClientAddress", ClientAddressResult },
            { "DateOfBirth", DateOfBirthResult },
            { "DepositAmount", DepositAmountResult },
            { "NationalIdImage", NationalIdImageResult }
        };
    }

    public class AttributeValidationResult
    {
        public string AttributeName { get; set; }
        public bool IsValid { get; set; }
        public string Errors { get; set; } = string.Empty;
        public string Warnings { get; set; } = string.Empty;

        // Helper method to add errors
        public void AddError(string error)
        {
            if (!string.IsNullOrEmpty(Errors))
                Errors += "; ";
            Errors += error;
        }

        // Helper method to add warnings
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrEmpty(Warnings))
                Warnings += "; ";
            Warnings += warning;
        }
    }

}
