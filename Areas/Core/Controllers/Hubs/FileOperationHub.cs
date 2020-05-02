using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Controllers.Helpers;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Infrastructure.Services;
using Serilog;

namespace PikaCore.Areas.Core.Controllers.Hubs
{
    public class FileOperationHub : Hub
    {
        private readonly IFileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public FileOperationHub(UserManager<ApplicationUser> userManager,
                                SignInManager<ApplicationUser> signInManager,
                                IFileService fileService,
                                IConfiguration configuration)
        {
            _fileService = fileService;
            _signInManager = signInManager;
            _userManager = userManager;
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
                var tmpListing = (await _fileService.ListPath(path));
                foreach (var absolutePath in tmpListing)
                {
                    var mappedPath = UnixHelper.MapToSystemPath(absolutePath);

                    if (!Directory.Exists(absolutePath)) continue;
                    try
                    {
                        if (UnixHelper.HasAccess(_configuration.GetSection("OsUser")["OsUsername"], absolutePath))
                        {
                            listing.Add(new string(mappedPath));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "FileOperationHub#List");
                    }
                }
            }

            if (_signInManager.IsSignedIn(Context.User))
            {
                Log.Information($"Returning a listing to a signed user: {Context.User.Identity.Name}");

                var user = await _userManager.GetUserAsync(Context.User);
                await this.Clients.User(user.Id).SendAsync("ReceiveListing", listing);
            }
            else
            {
                Log.Information("Returning a listing to unknown user.");

                await this.Clients.All.SendAsync("ReceiveListing", listing);
            }
        }
    }
}