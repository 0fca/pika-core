using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using FMS2.Models;

namespace FMS2.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Download() {
            return RedirectToAction("Error", new ErrorViewModel{ ErrorCode = 501, Message="This page is under construction at the moment, sorry." , RequestId = HttpContext.TraceIdentifier, Url = HttpContext.Request.Path });
        }

        public IActionResult Error(ErrorViewModel errorViewModel)
        {
            if (errorViewModel != null)
            {
                return View(errorViewModel);
            }
            else
            {
                return View(nameof(Index));
            }
        }

        public IActionResult ErrorByCode(int id) {
            return RedirectToAction("Error",new ErrorViewModel { ErrorCode = id, Message = "HTTP/1.1 "+id, RequestId = HttpContext.TraceIdentifier, Url = HttpContext.Request.Path });
        }
    }
}
