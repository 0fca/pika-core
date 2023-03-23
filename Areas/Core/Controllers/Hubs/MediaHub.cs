using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace PikaCore.Areas.Core.Controllers.Hubs
{
    public class MediaHub : Hub
    {
        public async Task CreateThumb(string systemPath, string guid, int s)
        {
        }
    }
}
