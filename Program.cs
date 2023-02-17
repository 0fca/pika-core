using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace PikaCore
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .Build();

            var port = ReadPortFromStdIn(args);

            var host = WebHost.CreateDefaultBuilder(args)
                .ConfigureKestrel((context, options) =>
                {
                    options.Limits.MaxRequestBodySize = 268435456;
                })
                .ConfigureLogging(l =>
                {
                    l.AddSerilog();
                    l.AddEventLog();
                })
                .UseStartup<Startup>()
                .UseConfiguration(configuration)
                .UseUrls($"http://0.0.0.0:{port}")
                .UseKestrel()
                .Build();
            host.Run();
        }

        private static int ReadPortFromStdIn(IReadOnlyList<string> args)
        {
            var port = 5000;
            try
            {
                port = int.Parse(args[0]);
            }
            catch
            {
                // ignored
            }
            return port;
        }
    }
}
