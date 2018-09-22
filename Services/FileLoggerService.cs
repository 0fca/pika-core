using FMS2.Loggers;
using FMS2.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FMS2.Services
{
    public class FileLoggerService : IFileLoggerService
    {
        private readonly ILoggerProvider fileLoggerProvider;
        private readonly ILogger _logger;

        public FileLoggerService(ILoggerProvider fileLoggerProvider) {
            this.fileLoggerProvider = fileLoggerProvider;
            _logger = fileLoggerProvider.CreateLogger("Production");
        }

        public void Cleanup()
        {
            ((FileLoggerProvider)fileLoggerProvider).IdleForCleanup();
        }

        public void LogToFileAsync(LogLevel logLevel, string address, string message) {
            _logger.Log(logLevel, address + " : " + message);
        }

        public IEnumerable<string> GetLogs()
        {
           return ((FileLoggerProvider)fileLoggerProvider).GetLogs();
        }
    }
}
