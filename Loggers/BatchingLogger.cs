using Microsoft.Extensions.Logging;
using System;
using System.Text;
using PikaCore.Extensions;

namespace PikaCore.Loggers
{
    public class BatchingLogger : ILogger
    {
        private readonly BatchingLoggerProvider _provider;
        private readonly string _category;

        public BatchingLogger(BatchingLoggerProvider loggerProvider, string categoryName)
        {
            _provider = loggerProvider;
            _category = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None)
            {
                return false;
            }
            return true;
        }

        public void Log<TState>(DateTimeOffset timestamp, LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var builder = new StringBuilder();
            builder.Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
            builder.Append(" [");
            builder.Append(logLevel.ToString());
            builder.Append("] ");
            builder.Append("[");
            builder.Append(state.GetType().FullName);
            builder.Append("] ");
            builder.Append(_category);
            builder.Append(": ");
            builder.AppendLine(formatter(state, exception));

            if (exception != null)
            {
                builder.AppendLine(exception.ToString());
            }

            _provider.AddMessage(timestamp, builder.ToString());
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {

            Log(DateTimeOffset.Now, logLevel, state, exception, formatter);
        }
    }
}
