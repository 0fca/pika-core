using System;
using System.Net;
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

        public async Task<IActionResult> Index()
        {
            var cookieOptions = new CookieOptions()
            {
                HttpOnly = true,
                Expires = DateTimeOffset.Now.Add(TimeSpan.FromDays(365))
            };
            var message = (await _messageService.GetLatestMessage());
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

        public IActionResult ErrorByCode(int id)
        {
            return RedirectToAction("Error", new ErrorViewModel { ErrorCode = id, Message = "HTTP/1.1 " + id, RequestId = HttpContext.TraceIdentifier, Url = HttpContext.Request.Path });
        }
    }
}
