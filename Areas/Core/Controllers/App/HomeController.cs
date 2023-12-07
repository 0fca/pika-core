using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Models;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Core.Controllers.App
{
    [Area("Core")]
    public class HomeController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly IConfiguration _configuration;

        public HomeController(IMessageService messageService, IConfiguration configuration)
        {
            _messageService = messageService;
            _configuration = configuration;
        }

        [ViewData] public string? InfoMessage { get; set; } = "";

        [Route("/[area]")]
        [Route("", Name = "CoreIndex")]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [Route("/[area]/[action]")]
        public IActionResult Error([FromQuery] ErrorViewModel? errorViewModel)
        {
            this.Response.StatusCode = errorViewModel.ErrorCode;
            if (errorViewModel.ContentType == MediaTypeNames.Application.Json)
            {
                return new JsonResult(new
                {
                    errorViewModel.Message,
                    errorViewModel.Url
                });
            }
            return View(errorViewModel);
        }

        [Route("/[area]/[action]/{id:int}")]
        public IActionResult Status(int id)
        {
            return RedirectToAction("Error",
                    new ErrorViewModel
                    {
                        ErrorCode = id,
                        Message = "No specific error information passed",
                        RequestId = HttpContext.TraceIdentifier,
                        Url = HttpContext.Request.Headers["Referer"],
                        ContentType = HttpContext.Request.ContentType
                    }
            );
        }

        [HttpGet]
        [Route("/[area]/[action]", Name = "SetLanguage")]
        [AllowAnonymous]
        public IActionResult SetLanguage([FromQuery] string culture, [FromQuery] string returnUrl = "/")
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    SameSite = SameSiteMode.Lax,
                    Domain = _configuration.GetSection("Auth")["CookieDomain"],
                    Secure = false
                }
            );

            return Redirect(string.IsNullOrEmpty(Request.Headers["Referer"].ToString())
                ? returnUrl
                : Request.Headers["Referer"].ToString()
            );
        }
    }
}