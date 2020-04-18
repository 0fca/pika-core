using Microsoft.Extensions.Logging;

namespace PikaCore.Areas.Infrastructure.Services
{
    public interface IFileLoggerService
    {
        void LogToFileAsync(LogLevel logLevel, string address, string message);
    }
}
