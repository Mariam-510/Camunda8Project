using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.ClientModels
{
    public class ApplicationForm
    {
        public string ApplicationId { get; set; }
        public string FullName { get; set; }
        public string NationalId { get; set; }
        public string ClientAddress { get; set; }
        public DateTime DateOfBirth { get; set; }
        public decimal DepositAmount { get; set; }
        public string NationalIdImage { get; set; }
        public string Email { get; set; }
        public string PhoneNo { get; set; }


    }
}
