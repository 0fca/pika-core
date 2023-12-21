using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Minio.Exceptions;
using OpenIddict.Abstractions;
using PikaCore.Areas.Identity.Models.AccountViewModels;
using PikaCore.Infrastructure.Security;

namespace PikaCore.Areas.Identity.Controllers;

[Area("Identity")]
[AllowAnonymous]
public class GatewayController : Controller
{
    private readonly IOidcService _oidcService;
    private readonly IStringLocalizer<GatewayController> _localizer;
    private readonly IConfiguration _configuration;

    public GatewayController(IOidcService service,
        IStringLocalizer<GatewayController> localizer,
        IConfiguration configuration)
    {
        _oidcService = service;
        _localizer = localizer;
        _configuration = configuration;
    }

    [HttpGet]
    [ActionName("Login")]
    public async Task<IActionResult> Login([FromQuery] string? returnUrl, [FromQuery] string? clientId = null)
    {
        if (!string.IsNullOrEmpty(clientId))
        {
            var token = await _oidcService.VerifyRemoteClientWithClientId(clientId);
            if (string.IsNullOrEmpty(token))
            {
                throw new AuthenticationException("The remote client appears to be not authorized to such call.");
            }
        }
        if (!HttpContext.Request.Cookies.ContainsKey(".AspNet.Identity"))
            return View(
                new LoginViewModel()
                {
                    ReturnUrl = returnUrl ?? "/Core"
                } 
            );
        
        TempData["ReturnMessage"] = _localizer.GetString("You appear to be already logged in").Value;
        return Redirect(returnUrl ?? "/Core");
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [ActionName("Login")]
    public async Task<IActionResult> Login(LoginViewModel loginViewModel)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnMessage"] =
                _localizer.GetString("Your username or password are not in valid format").Value;
            return View();
        }

        try
        {
            var token = await _oidcService.GetAccessToken(loginViewModel);
            
            this.HttpContext.Response.Cookies.Append(".AspNet.Identity", token, new CookieOptions
                {
                    Secure = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Domain = _configuration.GetSection("Auth")["CookieDomain"]
                });
            var expiryDate = DateTime.Now.AddHours(1);
            this.HttpContext.Response.Cookies.Append(".Temp.AspNet.Identity", token, new CookieOptions
            {
                Secure = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Domain = _configuration.GetSection("Auth")["CookieDomain"],
                Expires = expiryDate
            });
        }
        catch (OpenIddictExceptions.ProtocolException e)
        {
            ViewData["ReturnMessage"] =
                _localizer.GetString($"Following error occurred: {e.ErrorDescription}").Value;
            return View();
        }

        return Redirect(loginViewModel.ReturnUrl ?? "/");
    }

    [HttpPost]
    [ActionName("Logout")]
    public async Task<IActionResult> Logout()
    {
        if (HttpContext.Request.Cookies.ContainsKey(".AspNet.Identity"))
        {
            HttpContext.Response.Cookies.Delete(".AspNet.Identity", new CookieOptions
            {
                Domain = _configuration.GetSection("Auth")["CookieDomain"],
                Path = "/"
            });
        }

        return Redirect("/Core");
    }
}