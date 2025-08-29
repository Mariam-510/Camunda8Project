using CamundaProject.Core.Interfaces.Services.Email;
using CamundaProject.Core.Models.EmailModels;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;

namespace CamundaProject.Application.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> SendEmailAsync(EmailModel request)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["EmailUsername"]));
                email.To.Add(MailboxAddress.Parse(request.To));
                email.Subject = request.Subject;
                email.Body = new TextPart(TextFormat.Html) { Text = request.Body };

                using var smtp = new SmtpClient();
                int port = _config.GetValue<int>("Port");

                await smtp.ConnectAsync(_config["EmailHost"], port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["EmailUsername"], _config["EmailPassword"]);

                var response = await smtp.SendAsync(email);

                await smtp.DisconnectAsync(true);

                return !string.IsNullOrEmpty(response);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
