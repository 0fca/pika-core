using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Core.Services;
using PikaCore.Areas.Infrastructure.Services;

namespace PikaCore.Areas.Core.Controllers.Hubs
{
    public class MediaHub : Hub
    {
        private readonly IMediaService _mediaService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MediaHub(IMediaService mediaService,
                        UserManager<ApplicationUser> userManager)
        {
            _mediaService = mediaService;
            _userManager = userManager;
        }


        public async Task CreateThumb(string systemPath, string guid, int s)
        {
            var thumbId = await _mediaService.CreateThumb(systemPath, guid, s);
            await Clients.User(_userManager.GetUserId(Context.User)).SendAsync("ReceiveThumb", !string.IsNullOrEmpty(thumbId), guid);
        }
    }
}
