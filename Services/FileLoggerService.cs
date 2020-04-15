using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace PikaCore.Services
{
    public class FileLoggerService : IFileLoggerService
    {
        private readonly ILoggerProvider _fileLoggerProvider;
        private readonly ILogger _logger;

        public FileLoggerService(ILoggerProvider fileLoggerProvider,
                                 IHostEnvironment env)
        {
            this._fileLoggerProvider = fileLoggerProvider;
            _logger = fileLoggerProvider.CreateLogger(env.EnvironmentName);
        }

        public void LogToFileAsync(LogLevel logLevel, string address, string message) => _logger.Log(logLevel, address + " : " + message);
    }
}
