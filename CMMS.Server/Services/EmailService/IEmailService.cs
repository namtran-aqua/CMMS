using System.Threading.Tasks;

namespace CMMS.Server.Services.EmailService
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
    }
}
