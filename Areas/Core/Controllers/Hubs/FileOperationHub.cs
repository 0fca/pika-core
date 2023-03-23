using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace PikaCore.Areas.Core.Controllers.Hubs
{
    public class FileOperationHub : Hub
    {
        private readonly IConfiguration _configuration;

        public FileOperationHub(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Copy()
        {

        }

        public void Move()
        {

        }

        public async Task List(string path)
        {
            var listing = new List<string>();

            if (!string.IsNullOrEmpty(path))
            {

            }
            Log.Information("Returning a listing to unknown user.");
            await this.Clients.All.SendAsync("ReceiveListing", listing);
        }
    }
}