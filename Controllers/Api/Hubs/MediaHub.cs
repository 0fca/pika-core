using FMS2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using PikaCore.Services;
using PikaCore.Services.Helpers;
using System;
using System.Threading.Tasks;

namespace PikaCore.Controllers.Api.Hubs
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


        public async Task CreateThumb(string systemPath, string guid)
        {
            guid = await _mediaService.CreateThumb(systemPath, guid);

            await Clients.User(_userManager.GetUserId(Context.User)).SendAsync("ReceiveThumb", guid);
        }
    }
}
