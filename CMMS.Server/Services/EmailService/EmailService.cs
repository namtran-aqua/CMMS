using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using CMMS.Shared.Dtos.Email;

namespace CMMS.Server.Services.EmailService
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

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            var flowUrl = _config["PowerAutomate:EmailFlowUrl"];
            if (string.IsNullOrWhiteSpace(flowUrl))
            {
                _logger.LogWarning($"Power Automate EmailFlowUrl is not configured. Logged mail body:\nTo: {toEmail}\nSubject: {subject}\nBody: {body}");
                return false;
            }

            try
            {
                var sendEmailDto = new SendEmailDto
                {
                    RequestId = Guid.NewGuid().ToString(),
                    ToAdress = toEmail,
                    Subject = subject,
                    Body = new StringBuilder(body)
                };

                var payload = new
                {
                    RequestId = 0,
                    Status = "No Status",
                    To = sendEmailDto.ToAdress,
                    sendEmailDto.Subject,
                    Body = sendEmailDto.Body.ToString()
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });

                _logger.LogInformation($"Sending JSON payload to Power Automate: {json}");

                using var client = new HttpClient();
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(flowUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email sent successfully to {toEmail} via Power Automate Flow.");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send email via Power Automate Flow. Status: {response.StatusCode}, Content: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending email via Power Automate Flow.");
                return false;
            }
        }
    }
}
