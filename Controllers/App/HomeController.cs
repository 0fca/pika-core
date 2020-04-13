using Microsoft.AspNetCore.Mvc;
using PikaCore.Models;

namespace PikaCore.Controllers.App
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
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
