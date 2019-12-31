using FMS2.Controllers;
using FMS2.Controllers.Helpers;
using FMS2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FMS.Controllers
{
    public class VideoController : Controller
    {
        private readonly IStreamingService _streamingService;

        public VideoController(IStreamingService streamingService)
        {
            _streamingService = streamingService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,FileManagerUser,User")]
        public IActionResult Watch(string path)
        {
            ViewData["Mime"] = MimeAssistant.GetMimeType(System.IO.Path.GetFileName(path));
            ViewData["VideoTitle"] = Path.GetFileNameWithoutExtension(UnixHelper.MapToPhysical(Constants.FileSystemRoot, path));

            if (path != null)
            {
                return View(nameof(Watch), path);
            }

            return NoContent();
        }

        [HttpGet]
        [Authorize(Roles = "Admin, FileManagerUser, User")]
        public async Task<ActionResult> Convert(string path)
        {
            Debug.WriteLine("Converting...");
            var str = await _streamingService.GetVideoByPath(UnixHelper.MapToPhysical(Constants.FileSystemRoot, path));
            return Ok(str != null);// new FileStreamResult();
        }

        [HttpGet]
        [Authorize(Roles = "Admin, FileManagerUser, User")]
        public async Task<FileStreamResult> Stream(string p)
        {
            var absolutePath = UnixHelper.MapToPhysical(Constants.FileSystemRoot, p);
            return File(await _streamingService.GetVideoByPath(absolutePath), MimeAssistant.GetMimeType(System.IO.Path.GetFileName(p)), true);
        }
    }
}
