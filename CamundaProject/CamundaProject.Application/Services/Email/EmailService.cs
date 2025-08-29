using CamundaProject.Core.Interfaces.Services.Email;
using CamundaProject.Core.Models.EmailModels;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;

namespace CamundaProject.Application.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(EmailModel request)
        {
            try
            {
                _logger.LogInformation("Preparing email to {Recipient}", request.To);

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["EmailUsername"]));
                email.To.Add(MailboxAddress.Parse(request.To));
                email.Subject = request.Subject;
                email.Body = new TextPart(TextFormat.Html) { Text = request.Body };

                using var smtp = new SmtpClient();
                int port = _config.GetValue<int>("Port");

                _logger.LogInformation("Connecting to SMTP server {Host}:{Port}", _config["EmailHost"], port);

                await smtp.ConnectAsync(_config["EmailHost"], port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["EmailUsername"], _config["EmailPassword"]);

                _logger.LogInformation("Sending email to {Recipient}", request.To);

                var response = await smtp.SendAsync(email);

                await smtp.DisconnectAsync(true);

                bool isSuccess = !string.IsNullOrEmpty(response);
                _logger.LogInformation("Email to {Recipient} {Status}", request.To, isSuccess ? "sent successfully" : "failed");

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending email to {Recipient}", request.To);
                return false;
            }
        }
    }
}
