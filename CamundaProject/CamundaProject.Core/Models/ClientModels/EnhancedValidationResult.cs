using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.ClientModels
{
    public class EnhancedValidationResult
    {
        public bool IsValid { get; set; }
        public AttributeValidationResult FullNameResult { get; set; }
        public AttributeValidationResult NationalIdResult { get; set; }
        public AttributeValidationResult ClientAddressResult { get; set; }
        public AttributeValidationResult DateOfBirthResult { get; set; }
        public AttributeValidationResult DepositAmountResult { get; set; }
        public AttributeValidationResult NationalIdImageResult { get; set; }
        public AttributeValidationResult EmailResult { get; set; }
        public AttributeValidationResult PhoneNoResult { get; set; }

        public Dictionary<string, AttributeValidationResult> AttributeResults => new()
    {
        { "FullName", FullNameResult },
        { "NationalId", NationalIdResult },
        { "ClientAddress", ClientAddressResult },
        { "DateOfBirth", DateOfBirthResult },
        { "DepositAmount", DepositAmountResult },
        { "NationalIdImage", NationalIdImageResult },
        { "Email", EmailResult },
        { "PhoneNo", PhoneNoResult }
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
