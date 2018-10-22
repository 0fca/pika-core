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

        public IActionResult About(int ID = 0, int day = 0)
        {
            ViewData["os.name"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
           
            ViewData["ver"] = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            if(ID != 16 + 11 || day != 25){
                return View();
            }else{
                return View("Dedication");
            }
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Projects() {
            return View();
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

        public IActionResult ErrorByCode(int ID) {
            return RedirectToAction("Error",new ErrorViewModel { ErrorCode = ID, Message = "HTTP/1.1 "+ID, RequestId = HttpContext.TraceIdentifier, Url = HttpContext.Request.Path });
        }
    }
}
