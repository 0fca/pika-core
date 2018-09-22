using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FMS2.Models;
using Microsoft.AspNetCore.Authorization;
using System.Runtime.InteropServices;
using System.Reflection;

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

        public IActionResult Error()
        {
            if (String.IsNullOrEmpty(HttpContext.Request.QueryString.Value))
            {
                return View();
            }
            else
            {
                return View(nameof(Index));
            }
        }
    }
}
