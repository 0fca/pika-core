using FMS2.Controllers;
using FMS2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using FMS2.Controllers.Helpers;

namespace FMS.Controllers
{
    public class VideoController : Controller
    {
        private readonly IStreamingService _streamingService;

        public VideoController(IStreamingService streamingService) {
            _streamingService = streamingService;
        }

        [HttpGet]
        [Authorize(Roles="Admin,FileManagerUser,User")] 
        public IActionResult Watch(string path)
        {
            if(path != null){
                return View(nameof(Watch),path);
            }

            return NoContent();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,FileManagerUser,User")]
        public async Task<ActionResult> Convert(string path) {
            Debug.WriteLine("Converting...");
            var str = await _streamingService.GetVideoByPath(UnixHelper.MapToPhysical(Constants.FileSystemRoot, path));
            return Ok(str != null);// new FileStreamResult();
        }

        [HttpGet]
        public IActionResult Test() {
            return Ok("Test.");
        }
    }
}