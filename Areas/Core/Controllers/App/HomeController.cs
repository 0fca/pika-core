using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PikaCore.Areas.Core.Models;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Core.Controllers.App
{
    [Area("Core")]
    [ResponseCache(CacheProfileName = "Default")]
    public class HomeController : Controller
    {
        private readonly IMessageService _messageService;
        public HomeController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [ViewData] public string? InfoMessage { get; set; } = "";
        
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

        public IActionResult Error(ErrorViewModel errorViewModel)
        {
            return errorViewModel != null ? View(errorViewModel) : View(nameof(Index));
        }

        public IActionResult Status(int id)
        {
            if (!System.IO.File.Exists($"_Partial/Errors/{id}.html"))
            {
                id = 404;
            }
            return RedirectToAction("Error", 
                new ErrorViewModel { 
                    ErrorCode = id, 
                    Message = "No specific error information", 
                    RequestId = HttpContext.TraceIdentifier, 
                    Url = HttpContext.Request.Headers["Referer"].ToString()
                }
            );
        }
    }
}
