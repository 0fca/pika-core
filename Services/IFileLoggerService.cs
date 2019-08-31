using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FMS2.Services
{
    public interface IFileLoggerService
    {
        void LogToFileAsync(LogLevel logLevel, string address, string message);
        void Cleanup();
        Task<List<string>> GetLogs();
    }
}
