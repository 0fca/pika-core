using System;
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
    [ResponseCache(CacheProfileName = "Default")]
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

        public IActionResult Test()
        {
            return Ok(MimeTypes.GetMimeType("Pacific_Rim_2_en.mp4"));
        }
        
        [Route("/[area]")]
        [Route("", Name = "CoreIndex")]
        public async Task<IActionResult> Index()
        {
            var cookieOptions = new CookieOptions()
            {
                HttpOnly = true,
                Expires = DateTimeOffset.Now.Add(TimeSpan.FromDays(365)),
                SameSite = SameSiteMode.Lax
            };
            
            var message = (await _messageService.GetLatestMessage());
            if (message == null) return View();
            var date = message.UpdatedAt;
            try
            {
                HttpContext.Request.Cookies.TryGetValue("InfoDate", out var dateTime);
                HttpContext.Request.Cookies.TryGetValue("InfoViewed", out var wasViewed);

                if (!bool.Parse(wasViewed) || date >= DateTime.FromBinary(long.Parse(dateTime)))
                {
                    InfoMessage = "There is new status update";
                    HttpContext.Response.Cookies.Append("InfoDate",
                        DateTime.Now.ToBinary().ToString(), cookieOptions);
                }
            }
            catch (ArgumentNullException ex)
            {
                InfoMessage = "There is new status update";
                HttpContext.Response.Cookies.Append("InfoDate",
                    date.ToBinary().ToString(),
                    cookieOptions);
            }

            HttpContext.Response.Cookies.Append("InfoViewed",
                "true",
                cookieOptions);

            return View();
        }
        
        [Route("/[area]/[action]")]
        public IActionResult Error([FromQuery] ErrorViewModel? errorViewModel)
        {
            errorViewModel!.Url = Request.Headers["Referer"];
            return errorViewModel != null ? View(errorViewModel) : View(nameof(Index));
        }
        
        [Route("/[area]/[action]/{id:int}")]
        public IActionResult Status(int id)
        {
            return RedirectToAction("Error", 
                new ErrorViewModel { 
                    ErrorCode = id, 
                    Message = "No specific error information passed", 
                    RequestId = HttpContext.TraceIdentifier, 
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
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), 
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
