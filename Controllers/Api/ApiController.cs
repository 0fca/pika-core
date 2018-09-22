using System;
using System.Threading.Tasks;
using FMS.Models;
using FMS2.Models;
using FMS2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FMS2.Controllers.Api
{
    [Produces("application/json")]
    public class ApiController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _loginManager;
        private readonly IFileLoggerService _loggerService;

        public ApiController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> loginManager, IFileLoggerService loggerService)
        {
            _userManager = userManager;
            _loginManager = loginManager;
            _loggerService = loggerService;
        }

        [Route("v1/api")]
        [AllowAnonymous]
        public IActionResult ApiIndex()
        {
            _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Viewing ApiIndex.");
            return View("/Views/Shared/ApiIndex.cshtml");
        }

        [Route("v1/api/login")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<LoginResultModel> AmeliaLogin()
        {
            byte[] buffer = new byte[(int)HttpContext.Request.ContentLength];
            int readCount = await HttpContext.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            LoginResultModel l = new LoginResultModel();
            string content = System.Text.Encoding.ASCII.GetString(buffer).Trim();
            LoginRequestModel mdl;

            if (readCount > 0)
            {
                mdl = JsonConvert.DeserializeObject<LoginRequestModel>(content);

                return Task<LoginResultModel>.Factory.StartNew(() => {
                    var user = _userManager.FindByEmailAsync(mdl.Mail).Result;
                    bool isOk = false;
                    if (user != null)
                    {
                        var signInResult = _loginManager.PasswordSignInAsync(user, mdl.Password, false, false);
                        isOk = signInResult.Result.Succeeded;
                    }

                    l.Success = isOk;
                    l.Code = isOk ? 0 : ((user != null && user.PasswordHash != null) ? 100 : 101);
                    l.Message = isOk ? "You've been successfully logged in." : "Bad credentials.";
                    _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Answering the request with: " + l.Code + " : " + l.Message);
                    return l;
                }).Result;
            }
            else
            {
                return Task<LoginResultModel>.Factory.StartNew(() => {
                    l.Success = false;
                    l.Code = -1;
                    l.Message = "Something went wrong while logging in.";
                    _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Answering the request with: " + l.Code + " : " + l.Message);
                    return l;
                }).Result;

            }
        }

        [Route("v1/api/logout")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<LoginResultModel> AmeliaLogout()
        {
            LoginResultModel l = null;
            try
            {
                await _loginManager.SignOutAsync();
                l = new LoginResultModel
                {
                    Success = true,
                    Message = "Logged out successfully.",
                    Code = 0
                };
            }
            catch (Exception e)
            {
                l = new LoginResultModel
                {
                    Success = false,
                    Message = "Couldn't log out because "+e.Message,
                    Code = 100
                };
            }
            
            return Task<LoginResultModel>.Factory.StartNew(() =>
            {
                _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Answering the request with: " + l.Code + " : " + l.Message);
                return l;
            }).Result;
        }
    }
}