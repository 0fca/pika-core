using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.Controllers
{
    public class VideoController : Controller
    {
        [HttpGet]
        [Authorize(Roles="Admin,User")] 
        [AutoValidateAntiforgeryToken] 
        public IActionResult Watch(string id){
            if(ViewData["srcvid"] != null){
                return View(id);
            }else{
                return NoContent();
            }
        }
    }
}