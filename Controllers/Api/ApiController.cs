using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FMS.Models;
using FMS2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FMS2.Controllers.Api
{
    [Produces("application/json")]
    public class ApiController : Controller
    {
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _loginManager;

        public ApiController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> loginManager) {
            _userManager = userManager;
            _loginManager = loginManager;
        }

        [Route("v1/api")]
        [AllowAnonymous]
        public IActionResult ApiIndex() {
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
                    return l;
                }).Result;
            }
            else
            {
                return Task<LoginResultModel>.Factory.StartNew(() => {
                    l.Success = false;
                    l.Code = -1;
                    l.Message = "Something went wrong while logging in.";
                    return l;
                }).Result;

            }
        }

        [Route("v1/api/logout")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<LoginResultModel> AmeliaLogout()
        {
            LoginResultModel l = new LoginResultModel();
            l.Success = true;
            l.Message = "Logged out successfully.";
            l.Code = 0;
            return Task<LoginResultModel>.Factory.StartNew(() =>
            {
                return l;
            }).Result;
        }
    }
}