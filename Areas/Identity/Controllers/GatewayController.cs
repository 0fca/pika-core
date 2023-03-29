﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
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
    public async Task<IActionResult> Login()
    {
        if (!HttpContext.Request.Cookies.ContainsKey(".AspNet.Identity")) return View();
        TempData["ReturnMessage"] = _localizer.GetString("You appear to be already logged in").Value;
        return Redirect("/Core");
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
                Expires = DateTimeOffset.Now.AddHours(
                    double.Parse(_configuration.GetSection("Auth")["AuthCookieExpiry"])
                ),
                SameSite = SameSiteMode.None,
                Domain = _configuration.GetSection("Auth")["CookieDomain"]
            });
        }
        catch (OpenIddictExceptions.ProtocolException e)
        {
            ViewData["ReturnMessage"] =
                _localizer.GetString($"Following error occurred: {e.ErrorDescription}").Value;
            return View();
        }

        return Redirect(loginViewModel.ReturnUrl);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [ActionName("Logout")]
    public async Task<IActionResult> Logout()
    {
        if (HttpContext.Request.Cookies.ContainsKey(".AspNet.Identity"))
        {
            HttpContext.Response.Cookies.Delete(".AspNet.Identity");
        }

        return Redirect("/Core");
    }
}