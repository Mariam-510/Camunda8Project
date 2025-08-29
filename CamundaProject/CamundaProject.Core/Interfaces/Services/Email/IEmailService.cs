using CamundaProject.Core.Models.EmailModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Interfaces.Services.Email
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailModel request);
    }
}
