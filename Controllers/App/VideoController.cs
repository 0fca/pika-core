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
        public IActionResult Video(int id){
            //string absol = TempData["src"].ToString();
            
            ViewData["srcvid"] = id;
            if(ViewData["srcvid"] != null){
                return View();
            }else{
                return NoContent();
            }
        }
    }
}