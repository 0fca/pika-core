using FMS2.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class VideoController : Controller
    {
        [HttpGet]
        public IActionResult Video(){
            //string absol = TempData["src"].ToString();
            
            ViewData["srcvid"] = TempData["src"];
            return View();
        }
    }
}