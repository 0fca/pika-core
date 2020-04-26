using System;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace PikaCore.Areas.Infrastructure.Services
{
    public interface IFileLoggerService
    {
        void LogToFileAsync(LogEventLevel logLevel, Exception e);
    }
}
