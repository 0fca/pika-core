using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
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

   public GatewayController(IOidcService service, IStringLocalizer<GatewayController> localizer)
   {
      _oidcService = service;
      _localizer = localizer;
   }
   
   [HttpGet]
   [ActionName("Login")]
   public async Task<IActionResult> Login()
   {
      if (!HttpContext.Request.Cookies.ContainsKey(".AspNet.ShrCk")) return View();
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
         this.HttpContext.Response.Cookies.Append(".AspNet.ShrCk", token);
      }
      catch (OpenIddictExceptions.ProtocolException e)
      {
         ViewData["ReturnMessage"] = 
            _localizer.GetString( $"Following error occurred: {e.ErrorDescription}").Value;
         return View();
      }
      return Redirect(loginViewModel.ReturnUrl);
   }

   [HttpPost]
   [AutoValidateAntiforgeryToken]
   [ActionName("Logout")]
   public async Task<IActionResult> Logout()
   {
      HttpContext.Response.Cookies.Delete(".AspNet.ShrCk");
      return Redirect("/Core");
   }
}