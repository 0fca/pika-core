using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Models.ContentViewModels;
using PikaCore.Infrastructure.Security;
using MimeAssistant = PikaCore.Infrastructure.Adapters.Filesystem.MimeAssistant;

namespace PikaCore.Areas.Core.Controllers.App
{
    [Area("Core")]
    public class ContentController : Controller
    {
        private readonly IdDataProtection _dataProtection;
        private readonly IConfiguration _configuration;
        
        public ContentController(IdDataProtection idDataProtection,
                                 IConfiguration configuration)
        {
            _dataProtection = idDataProtection;
            _configuration = configuration;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("/[area]/[controller]/{fileId?}")]
        public async Task<IActionResult> Index(string fileId, string returnUrl)
        {
            var physicalPath = _dataProtection.Decode(fileId);
            if (Directory.Exists(physicalPath))
            {
                TempData["ReturnMessage"] = "Couldn't view a directory.";
                return RedirectToRoute("/Core/Storage/Browse");
            }
            
            var contentViewModel = new ContentViewModel()
            {
                PresentableName = 
                    Directory.Exists(physicalPath) 
                        ? Path.GetDirectoryName(physicalPath) 
                        : Path.GetFileName(physicalPath),
                TempFileId = null,
                DataType = MimeAssistant.GetMimeType(physicalPath),
                ReturnUrl = returnUrl
            };

            return View(contentViewModel);
        }
    }
}