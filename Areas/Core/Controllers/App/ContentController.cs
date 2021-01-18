using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Models.ContentViewModels;
using PikaCore.Infrastructure.Security;
using PikaCore.Infrastructure.Services;
using PikaCore.Infrastructure.Services.Helpers;

namespace PikaCore.Areas.Core.Controllers.App
{
    [Area("Core")]
    public class ContentController : Controller
    {
        private readonly IdDataProtection _dataProtection;
        private readonly IStaticContentService _contentService;
        private readonly IConfiguration _configuration;
        
        public ContentController(IdDataProtection idDataProtection,
                                 IStaticContentService fileService,
                                 IConfiguration configuration)
        {
            _dataProtection = idDataProtection;
            _contentService = fileService;
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
            
            if (_contentService.IsInCdn(physicalPath))
            {
                contentViewModel.TempFileId = "/Static/" + _contentService.RetrieveFromCdn(physicalPath);
                return View(contentViewModel);
            }

            contentViewModel.TempFileId = "/Static/" + await _contentService.CopyToCdn(physicalPath);
            return View(contentViewModel);
        }
        
         ~ContentController()
        {
            _contentService.CleanCdn();   
        }
    }
}