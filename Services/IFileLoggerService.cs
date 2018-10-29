using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
