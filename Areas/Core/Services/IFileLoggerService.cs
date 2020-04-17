using Microsoft.Extensions.Logging;

namespace PikaCore.Areas.Core.Services
{
    public interface IFileLoggerService
    {
        void LogToFileAsync(LogLevel logLevel, string address, string message);
    }
}
