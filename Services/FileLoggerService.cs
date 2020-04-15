using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using FileLoggerProvider = Pomelo.Logging.FileLogger.FileLoggerProvider;

namespace PikaCore.Services
{
    public class FileLoggerService : IFileLoggerService
    {
        private readonly FileLoggerProvider _fileLoggerProvider;
        private readonly ILogger _logger;

        public FileLoggerService(ILoggerProvider fileLoggerProvider,
                                 IHostEnvironment env)
        {
            this._fileLoggerProvider = (FileLoggerProvider)fileLoggerProvider;
            _logger = fileLoggerProvider.CreateLogger(env.EnvironmentName);
        }

        public void LogToFileAsync(LogLevel logLevel, string address, string message) => _logger.Log(logLevel, address + " : " + message);
    }
}
