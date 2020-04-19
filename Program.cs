using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

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
                .UseStartup<Startup>()
                .UseConfiguration(configuration)
                .UseUrls($"http://localhost:{port}")
                .UseKestrel()
                .UseSockets(opts => { opts.NoDelay = true; })
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
