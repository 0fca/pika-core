using FMS2.Controllers.Helpers;
using FMS2.Models;
using FMS2.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FMS2.Controllers.Api.Hubs
{
    public class FileOperationHub : Hub
    {
        private readonly IFileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IFileLoggerService _fileLoggerService;
        private readonly IConfiguration _configuration;

        public FileOperationHub(UserManager<ApplicationUser> userManager,
                                SignInManager<ApplicationUser> signInManager,
                                IFileService fileService,
                                IFileLoggerService fileLoggerService,
                                IConfiguration configuration)
        {
            _fileService = fileService;
            _signInManager = signInManager;
            _userManager = userManager;
            _fileLoggerService = fileLoggerService;
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
                    _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", "Checking this directory: " + absolutePath);
                    if (Directory.Exists(absolutePath))
                    {
                        try
                        {
                            if (UnixHelper.HasAccess(_configuration.GetSection("OsUser")["OsUsername"], absolutePath))
                            {
                                _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", "System has access to this resource: " + new string(mappedPath));
                                listing.Add(new string(mappedPath));
                            }
                        }
                        catch (Exception e)
                        {
                            _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Error, "localhost", e.Message);
                        }
                    }
                }
            }

            if (_signInManager.IsSignedIn(Context.User))
            {
                _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", "Returning results to client signed in user.");

                var user = await _userManager.GetUserAsync(Context.User);
                await this.Clients.User(user.Id).SendAsync("ReceiveListing", listing);
            }
            else
            {
                _fileLoggerService.LogToFileAsync(Microsoft.Extensions.Logging.LogLevel.Information, "localhost", "Returning results to not signed in user.");

                await this.Clients.All.SendAsync("ReceiveListing", listing);
            }
        }
    }
}