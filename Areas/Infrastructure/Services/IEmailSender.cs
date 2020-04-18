using System.Threading.Tasks;

namespace PikaCore.Areas.Infrastructure.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
