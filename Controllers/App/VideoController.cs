using PikaCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using PikaCore.Controllers.Helpers;

namespace PikaCore.Controllers
{
    public class VideoController : Controller
    {
        private readonly IStreamingService _streamingService;
        private readonly IFileService _fileService;

        public VideoController(IStreamingService streamingService,
                               IFileService fileService)
        {
            _streamingService = streamingService;
            _fileService = fileService;

        }

        [HttpGet]
        [Authorize(Roles = "Admin,FileManagerUser,User")]
        public IActionResult Watch(string path)
        {
            ViewData["Mime"] = MimeAssistant.GetMimeType(_fileService.RetrieveAbsoluteFromSystemPath(path));
            
            ViewData["VideoTitle"] = Path.GetFileName(_fileService.RetrieveAbsoluteFromSystemPath(path));

            if (path != null)
            {
                return View(nameof(Watch), path);
            }

            return NoContent();
        }

        [HttpGet]
        [Authorize(Roles = "Admin, FileManagerUser, User")]
        public ActionResult Convert(string path)
        {
            Debug.WriteLine("Converting...");
            var str = _streamingService.GetVideoByPath(_fileService.RetrieveAbsoluteFromSystemPath(path));
            return Ok(str != null);// new FileStreamResult();
        }

        [HttpGet]
        [Authorize(Roles = "Admin, FileManagerUser, User")]
        public FileStreamResult Stream(string p)
        {
            var absolutePath = _fileService.RetrieveAbsoluteFromSystemPath(p);
            return File(_streamingService.GetVideoByPath(absolutePath), MimeAssistant.GetMimeType(absolutePath), true);
        }
    }
}
