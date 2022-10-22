using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Pika.Domain.Identity.Data;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Core.Services;
using PikaCore.Infrastructure.Services;
using Serilog;

namespace PikaCore.Areas.Core.Controllers.Hubs
{
    public class MediaHub : Hub
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public MediaHub(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }


        public async Task CreateThumb(string systemPath, string guid, int s)
        {
        }
    }
}
