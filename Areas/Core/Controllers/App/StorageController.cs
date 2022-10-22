using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Pika.Domain.Identity.Data;
using Pika.Domain.Storage.Data;
using PikaCore.Areas.Core.Controllers.Hubs;
using PikaCore.Areas.Core.Data;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Services;
using PikaCore.Infrastructure.Security;
using Serilog;

namespace PikaCore.Areas.Core.Controllers.App
{
    [Area("Core")]
    [ResponseCache(CacheProfileName = "Default")]
    public class StorageController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUrlGenerator _urlGeneratorService;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _storageIndexContext;
        private readonly IHubContext<StatusHub> _hubContext;
        private readonly IdDataProtection _idDataProtection;
        private readonly IStringLocalizer<StorageController> _stringLocalizer;

        #region TempDataMessages

        [TempData(Key = "showGenerateUrlPartial")]
        public bool ShowGenerateUrlPartial { get; set; }
        
        [TempData(Key = "returnMessage")] 
        public string ReturnMessage { get; set; } = "";

        #endregion

        public StorageController(SignInManager<ApplicationUser> signInManager,
               IUrlGenerator iUrlGenerator,
               ApplicationDbContext storageIndexContext,
               IHubContext<StatusHub> hubContext,
               IConfiguration configuration,
               IdDataProtection idDataProtection,
               IStringLocalizer<StorageController> stringLocalizer)
        {
            _signInManager = signInManager;
            _urlGeneratorService = iUrlGenerator;
            _storageIndexContext = storageIndexContext;
            _hubContext = hubContext;
            _configuration = configuration;
            _idDataProtection = idDataProtection;
            _stringLocalizer = stringLocalizer;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            return RedirectToActionPermanent(nameof(Browse));
        }

        [HttpGet(Name = "Browse")]
        [ActionName("Browse")]
        [AllowAnonymous]
        public async Task<IActionResult> Browse(string? path, int offset = 0, int count = 10)
        {
            var lrmv = new FileResultViewModel();
            
            return View(lrmv);
        }

        [HttpGet(Name = "ResourceInformation")]
        [AllowAnonymous]
        public IActionResult ResourceInformation(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("An id cannot be empty");
            }

            var path = _idDataProtection.Decode(id);
            var model = new ResourceInformationViewModel()
            {
                IsHidden = false 
            };

            
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, FileManagerUser, User")]
        [Route("/[controller]/[action]/{name?}")]
        public async Task<IActionResult> GenerateUrl(string name, string returnUrl)
        {
            var entryName = _idDataProtection.Decode(name);
            var offset = int.Parse(Get("Offset"));
            var count = int.Parse(Get("Count"));
            
            if (!string.IsNullOrEmpty(entryName))
            {
                ShowGenerateUrlPartial = false;
                try
                {
                    var s = _storageIndexContext.IndexStorage.ToList().Find(record => record.AbsolutePath.Equals(entryName));

                    if (s == null)
                    {
                        s = new StorageIndexRecord
                        {
                            AbsolutePath = entryName,
                            Urlhash = _urlGeneratorService.GenerateId(entryName),
                            UserId = await IdentifyUser(),
                            Expires = true
                        };
                    }
                    else if (s.ExpireDate.Date <= DateTime.Now.Date)
                    {
                        s.ExpireDate = StorageIndexRecord.ComputeDateTime();
                    }
                    _storageIndexContext.Update(s);
                    await _storageIndexContext.SaveChangesAsync();

                    TempData["urlhash"] = s.Urlhash;
                    ShowGenerateUrlPartial = true;
                    var port = HttpContext.Request.Host.Port;
                    TempData["host"] = HttpContext.Request.Host.Host + 
                                       (port != null ? ":" + HttpContext.Request.Host.Port : "");
                    TempData["protocol"] = "https";
                    TempData["returnUrl"] = returnUrl;
                    return RedirectToAction(nameof(Browse), new { @path = returnUrl, offset, count });
                }
                catch (InvalidOperationException ex)
                {
                    Log.Error(ex, "StorageController#GenerateUrl");
                    ReturnMessage = ex.Message;
                    return RedirectToAction(nameof(Browse), new { @path = returnUrl, offset, count });
                }
            }

            ReturnMessage = _stringLocalizer.GetString("Couldn't generate token for that resource").Value;
            return RedirectToAction(nameof(Browse), new { @path = returnUrl });
        }
        
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Download(string id, string returnUrl, bool z = false)
        {
            id = !z ? _idDataProtection.Decode(id) : id;
            int offset = int.Parse(Get("Offset"));
            int count = int.Parse(Get("Count"));


            ReturnMessage = _stringLocalizer.GetString("Resource id cannot be null").Value;
            return RedirectToAction(nameof(Browse), new {@path = returnUrl, offset, count});
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Thumb(string id)
        {
            return StatusCode(501);
        }
        
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [AllowAnonymous]
        [RequestFormLimits(MultipartBodyLengthLimit = 268435456)]
        public async Task<IActionResult> Upload(List<IFormFile> files, string returnPath = "")
        {
            return StatusCode(501);
        }
        
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Archive(string id)
        {
            var absolutePath = _idDataProtection.Decode(id);
            
            ReturnMessage = _stringLocalizer.GetString("Your request has been accepted").Value;
            return StatusCode(501);
        }
        
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin, FileManagerUser")]
        public async Task<IActionResult> Create(string name, string returnUrl)
        {
            var pattern = new Regex(@"\W|_");
            var offset = int.Parse(Get("Offset"));
            var count = int.Parse(Get("Count"));
            
            if (!pattern.Match(name).Success)
            {
                try
                {

                    ReturnMessage = _stringLocalizer
                        .GetString("Successfully created directory:").Value + "";
                    Log.Information(ReturnMessage);
                    return RedirectToAction(nameof(Browse), new { path = returnUrl });
                }
                catch (Exception e)
                {
                    Log.Error(e, "StorageController#Create");
                    ReturnMessage = _stringLocalizer.GetString( "Error: Couldn't create directory").Value;
                    return RedirectToAction(nameof(Browse), new { path = returnUrl, offset, count });
                }
            }

            ReturnMessage = _stringLocalizer
                .GetString("You cannot use non-alphabetic characters in directory names").Value;
            return RedirectToAction(nameof(Browse), new { path = returnUrl, offset, count });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string currentPath)
        {
            var offset = int.Parse(string.IsNullOrEmpty(Get("Offset")) ? "0" : Get("Offset"));
            var count = int.Parse(string.IsNullOrEmpty(Get("Count")) ? "0" : Get("Count"));
            var toBeDeletedItemsModel = new DeleteResourcesViewModel()
            {
                ReturnPath = currentPath
            };
            toBeDeletedItemsModel.ApplyPaging(offset, count);

            return View(toBeDeletedItemsModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> DeleteConfirmation(DeleteResourcesViewModel deleteResourcesViewModel)
        {
            var contents = deleteResourcesViewModel.ToBeDeletedItems;
            var returnPath = deleteResourcesViewModel.ReturnPath;
            var offset = int.Parse(Get("Offset"));
            var count = int.Parse(Get("Count"));
            if (contents.Count > 0)
            {
                try
                {

                    ReturnMessage = _stringLocalizer.GetString( "Successfully deleted elements").Value;
                    Log.Information(ReturnMessage);
                    return RedirectToAction(nameof(Browse), new { path = returnPath, offset,  count});
                }
                catch
                { 
                    ReturnMessage = _stringLocalizer.GetString("Error: Couldn't delete resource").Value;
                    return RedirectToAction(nameof(Browse), new { path =  returnPath,  offset,  count});
                }
            }

            ReturnMessage = _stringLocalizer.GetString("Error: Nothing to be deleted").Value;
            return RedirectToAction(nameof(Browse), new { path = returnPath });
        }

        [HttpGet]
        [Authorize(Roles = "Admin, FileManagerUser")]
        [AutoValidateAntiforgeryToken]
        public IActionResult Rename(string n)
        {
            var name = "";
            var rfm = new RenameFileModel
            {
                IsDirectory = IsDirectory(name),
                OldName = name,
                AbsoluteParentPath = Directory.GetParent(name).FullName,
                ReturnUrl = Directory.GetParent(n).Name
            };
            Log.Information("Directory created.");

            return View(rfm);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin, FileManagerUser")]
        public IActionResult Rename(RenameFileModel rfm)
        {
            if (!string.IsNullOrEmpty(rfm.NewName))
            {
                var isRenamed = false; 
                if (!isRenamed)
                {
                    ReturnMessage = "Couldn't renamed to " + rfm.NewName;
                    return RedirectToAction(nameof(Browse),
                        new {path = rfm.ReturnUrl});
                }
                ReturnMessage = string.Format(_stringLocalizer.GetString( "Successfully renamed to {0}").Value, rfm.NewName);

                return RedirectToAction(nameof(Browse), 
                    new { path = rfm.ReturnUrl });

            }
            ModelState.AddModelError(HttpContext.TraceIdentifier, 
                _stringLocalizer.GetString( "New name cannot be empty"));
            return View(rfm);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [AutoValidateAntiforgeryToken]
        [Route("Storage/Hide/{systemPath}")]
        public IActionResult Hide(string systemPath, string returnPath)
        {
            var offset = int.Parse(string.IsNullOrEmpty(Get("Offset")) ? "0" : Get("Offset"));
            var count = int.Parse(string.IsNullOrEmpty(Get("Count")) ? "0" : Get("Count"));
            systemPath = _idDataProtection.Decode(systemPath);
            Log.Information($"{systemPath}");
            try
            {
                
                ReturnMessage = true ? 
                    _stringLocalizer.GetString( "Resource hidden").Value : 
                    _stringLocalizer.GetString( "Couldn't be hidden").Value;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
            return RedirectToAction(nameof(Browse), new {@path = returnPath, offset, count});
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [AutoValidateAntiforgeryToken]
        [Route("Storage/Show/{systemPath}")]
        public IActionResult Show(string systemPath, string returnPath)
        {
            var offset = int.Parse(string.IsNullOrEmpty(Get("Offset")) ? "0" : Get("Offset"));
            var count = int.Parse(string.IsNullOrEmpty(Get("Count")) ? "0" : Get("Count"));
            systemPath = _idDataProtection.Decode(systemPath);
            Log.Information($"{systemPath}");
            try
            {
                var isHidden = true; 
                ReturnMessage = isHidden 
                    ? _stringLocalizer.GetString( "Resource is visible now").Value 
                    : _stringLocalizer.GetString( "Couldn't be set visible").Value;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
            return RedirectToAction(nameof(Browse), new {@path = returnPath, offset, count});
        }

        #region HelperMethods

        private async Task<string> IdentifyUser()
        {
            var user = await _signInManager.UserManager.GetUserAsync(HttpContext.User);
            return user != null
                ? await _signInManager.UserManager.GetEmailAsync(user)
                : HttpContext.Connection.RemoteIpAddress.ToString();
        }

        public void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
        }

        private static bool IsDirectory(string name)
        {
            return !System.IO.File.Exists(name);
        }

        #endregion

        #region CookieHelperMethods

        private void Set(string key, string value, int? expireTime)
        {
            var option = new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = expireTime.HasValue
                    ? DateTime.Now.AddMinutes(expireTime.Value)
                    : DateTime.Now.AddMilliseconds(10)
            };
            
            Response.Cookies.Append(key, value, option);
        }

        private void SetPagingParams(int offset, int count, int pageCount)
        {
            TempData["Offset"] = offset;
            TempData["Count"] = count;
            TempData["PageCount"] = pageCount;
        }
        
        private string Get(string key)
        {
            return HttpContext.Request.Cookies[key];
        }
        
        #endregion
    }
}
