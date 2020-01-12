using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace FMS2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
            .AddCommandLine(args)
            .AddEnvironmentVariables()
            .Build();
            var port = 5000;
            if (args.Length > 0 && args[0] != null)
            {
                port = int.Parse(args[0]);
            }

            var host = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseConfiguration(configuration)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"))
                    .AddDebug();
                })
	        .UseUrls($"http://localhost:{port}")
		.UseKestrel()
                .Build();

            host.Run();
        }
    }
}
