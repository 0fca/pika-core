using FMS2.Providers;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FMS2.Services
{
    public class FileLoggerService : IFileLoggerService
    {
        private readonly ILoggerProvider _fileLoggerProvider;
        private readonly ILogger _logger;

        public FileLoggerService(ILoggerProvider fileLoggerProvider) {
            this._fileLoggerProvider = fileLoggerProvider;
            _logger = fileLoggerProvider.CreateLogger("Production");
        }

        public void Cleanup() => ((FileLoggerProvider)_fileLoggerProvider).IdleForCleanup();

        public void LogToFileAsync(LogLevel logLevel, string address, string message) => _logger.Log(logLevel, address + " : " + message);

        public async Task<List<string>> GetLogs() => await ((FileLoggerProvider)_fileLoggerProvider).GetLogs();
    }
}
