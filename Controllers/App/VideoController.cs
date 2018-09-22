using FMS2.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.Controllers
{
    public class VideoController : Controller
    {
        [HttpGet]
        [Authorize(Roles="Admin,User")] 
        [ValidateAntiForgeryToken] 
        public IActionResult Video(){
            //string absol = TempData["src"].ToString();
            
            ViewData["srcvid"] = TempData["src"];
            if(ViewData["srcvid"] != null){
                return View();
            }else{
                return NoContent();
            }
        }
    }
}