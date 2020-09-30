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
using Microsoft.Extensions.FileProviders;
using PikaCore.Areas.Core.Controllers.Helpers;
using PikaCore.Areas.Core.Controllers.Hubs;
using PikaCore.Areas.Core.Data;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Core.Models.File;
using PikaCore.Areas.Core.Services;
using PikaCore.Areas.Core.Services.Jobs;
using PikaCore.Areas.Infrastructure.Services;
using PikaCore.Areas.Infrastructure.Services.Helpers;
using PikaCore.Security;
using Quartz;
using Serilog;

namespace PikaCore.Areas.Core.Controllers.App
{
    [Area("Core")]
    public class StorageController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IFileService _fileService;
        private readonly IUrlGenerator _urlGeneratorService;
        private readonly IConfiguration _configuration;
        private readonly StorageIndexContext _storageIndexContext;
        private readonly IHubContext<StatusHub> _hubContext;
        private readonly IdDataProtection _idDataProtection;
        private readonly IJobService _jobService;

        #region TempDataMessages

        [TempData(Key = "showGenerateUrlPartial")]
        public bool ShowGenerateUrlPartial { get; set; }
        
        [TempData(Key = "returnMessage")] 
        public string ReturnMessage { get; set; } = "";

        #endregion

        public StorageController(SignInManager<ApplicationUser> signInManager,
               IFileService fileService, 
               IUrlGenerator iUrlGenerator,
               StorageIndexContext storageIndexContext,
               IHubContext<StatusHub> hubContext,
               IConfiguration configuration,
               IdDataProtection idDataProtection,
               IJobService jobService)
        {
            _signInManager = signInManager;
            _fileService = fileService;
            _urlGeneratorService = iUrlGenerator;
            _storageIndexContext = storageIndexContext;
            _hubContext = hubContext;
            _configuration = configuration;
            _idDataProtection = idDataProtection;
            _jobService = jobService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            return RedirectToActionPermanent(nameof(Browse));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Browse(string? path, int offset = 0, int count = 50)
        {
            var osUser = _configuration.GetSection("OsUser")["OsUsername"];
            
            if (string.IsNullOrEmpty(path)
            || !UnixHelper.HasAccess(osUser, 
                _fileService.RetrieveAbsoluteFromSystemPath(path)))
            {
                path = Path.DirectorySeparatorChar.ToString();
            }
            
            var contents = GetContents(path);

            ViewData["path"] = path;
            ViewData["returnUrl"] =  UnixHelper.GetParent(path);
            var lrmv = new FileResultViewModel();

            if (null == contents)
            {
                ReturnMessage = "Something went wrong...";
                return View(lrmv);
            }

            if (!contents.Exists)
            {
                ReturnMessage = "The resource doesn't exist on the filesystem.";
                return View(lrmv);
            }
            
            lrmv.Contents = contents;
            lrmv.ContentsList = contents.ToList();

            await lrmv.SortContents();
            var fileInfosList = lrmv.ContentsList;

            var pageCount = fileInfosList.Count / count;

            SetPagingParams(offset, count, pageCount);

            Set("Offset", offset.ToString(), 3600);
            Set("Count", count.ToString(), 3600);
            Set("PageCount", pageCount.ToString(), 3600);
                
            if (fileInfosList.Count > count)
            {
                lrmv.ApplyPaging(offset, count);
            }
                
            if (!HttpContext.User.IsInRole("Admin"))
            {
                try
                {
                    lrmv.ApplyAcl(osUser);
                }
                catch (InvalidOperationException ex)
                {
                    Log.Error(ex, "StorageController#Browse");
                }
            }
                
            var fileInfo = _fileService.RetrieveFileInfoFromSystemPath(path);
            if (fileInfo.Exists 
            && !fileInfo.IsDirectory)
            {
                return RedirectToAction(nameof(Download), new { @id = fileInfo.Name, @z = true });
            }
            
            return View(lrmv);
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

            ReturnMessage = "Couldn't generate token for that resource.";
            return RedirectToAction(nameof(Browse), new { @path = returnUrl });
        }

       
        [HttpGet]
        [AllowAnonymous]
        [Route("/[controller]/[action]/{id?}")]
        public ActionResult PermanentDownload(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                StorageIndexRecord? s = null;
                try
                {
                    s = _storageIndexContext.IndexStorage.SingleOrDefault(record => record.Urlhash.Equals(id));

                    if (s != null)
                    {
                        if (!s.Expires || (s.ExpireDate != DateTime.Now && s.ExpireDate > DateTime.Now))
                        {
                                var fileBytes = _fileService.AsStreamAsync(s.AbsolutePath);
                                var name = Path.GetFileName(s.AbsolutePath);
                                if (fileBytes != null)
                                {
                                    return File(fileBytes, MimeAssistant.GetMimeType(name), name, true);
                                }
                                
                                ReturnMessage = "Couldn't read requested resource: " + s.Urlid;
                                Log.Warning(ReturnMessage);
                                return RedirectToAction(nameof(Browse));
                        }
                        ReturnMessage = "It seems that this url expired, you need to generate a new one.";
                        return RedirectToAction(nameof(Browse));
                    }
                    ReturnMessage = "It seems that given token doesn't exist in the database.";
                    return RedirectToAction(nameof(Browse));
                }
                catch (InvalidOperationException ex)
                {
                    ReturnMessage = s != null 
                        ? "Couldn't read requested resource: " + s.Urlid 
                        : "Database error occured.";
                    Log.Error(ex, string.Concat(ReturnMessage, "StorageController#PermanentDownload"));
                    return RedirectToAction(nameof(Browse));
                }
            }
            ReturnMessage = "No id given or database is down.";
            return RedirectToAction(nameof(Browse));
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Download(string id,  string returnUrl, bool z = false)
        {
            
            id = !z ? _idDataProtection.Decode(id) : id;
            int offset = int.Parse(Get("Offset"));
            int count = int.Parse(Get("Count"));
            
            try{
                if (!string.IsNullOrEmpty(id))
                {
                    var fileInfo = _fileService.RetrieveFileInfoFromAbsolutePath(id); 
                    var path = fileInfo.PhysicalPath;
                    Log.Information($"Decoded path: {path}");
                    if (!fileInfo.Exists)
                    {
                        ReturnMessage = "File doesn't exist on server's filesystem.";
                        return RedirectToAction(nameof(Browse), new { @path = returnUrl });
                    }
                    
                    if (Directory.Exists(path))
                    {
                        ReturnMessage = "This is a folder, cannot download it directly.";
                        return RedirectToAction(nameof(Browse), new { @path = returnUrl });
                    }
                    
                    var fs =  _fileService.AsStreamAsync(path);
                    var mime = MimeAssistant.GetMimeType(path);
                    return File(fs, mime, fileInfo.Name);
                }
            }catch(Exception e){
                Log.Error(e, "StorageController#Download");
            }
            ReturnMessage = "Resource id cannot be null.";
            return RedirectToAction(nameof(Browse), new { @path = returnUrl, offset, count });

        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Thumb(string id)
        {
            Log.Information($"Request for thumb of id: {id}");
            var format = _configuration.GetSection("Images")["Format"].ToLower();
            var thumbFileName = $"{id}.{format}";
            var absoluteThumbPath = Path.Combine(_configuration.GetSection("Images")["ThumbDirectory"],
                                                 thumbFileName
                                                 );
            var thumbFileStream = _fileService.AsStreamAsync(absoluteThumbPath);
            return File(thumbFileStream, "image/jpeg");
        }
        
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [AllowAnonymous]
        [RequestFormLimits(MultipartBodyLengthLimit = 268435456)]
        public async Task<IActionResult> Upload(List<IFormFile> files, string returnPath = "")
        {
            try
            {
                var size = files.Sum(f => f.Length);
                var returnMessage = files.Count 
                                    + (files.Count == 1 ? "file" : " files ") 
                                    + $" uploaded of summary size "
                                    + UnixHelper.DetectUnitBySize(size);
                var (checkResultMessage, tmpFilesList) = 
                    await _fileService.SanitizeFileUpload(files, 
                            returnPath,
                    HttpContext.User.IsInRole("Admin"));

                returnMessage = string.IsNullOrEmpty(checkResultMessage) ? returnMessage : checkResultMessage;
                if (!string.IsNullOrEmpty(checkResultMessage))
                {
                    return StatusCode(403, checkResultMessage);
                }

                await _fileService.PostSanitizeUpload(tmpFilesList);
                Log.Warning($"Accepted to be created at path: /Core/Storage/Browse?path={returnPath}");
                return Accepted($"/Core/Storage/Browse?path={returnPath}", returnMessage);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
        
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Archive(string id)
        {
            var absolutePath = _idDataProtection.Decode(id);
            var jobDataMap = new JobDataMap
            {
                {"userId", _signInManager.UserManager.GetUserId(HttpContext.User)},
                {"output", _configuration["Paths:zip-tmp"]},
                {"absolutePath", absolutePath}
            };
            var name = await _jobService.CreateJob<ArchiveJob>(jobDataMap);
            ReturnMessage = "Your request has been accepted.";
            return RedirectPermanent($"/Core/Jobs/Submit?name={name}");
        }
        
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin, FileManagerUser")]
        public async Task<IActionResult> Create(string name, string returnUrl)
        {
            var pattern = new Regex(@"\W|_");
            int offset = int.Parse(Get("Offset"));
            int count = int.Parse(Get("Count"));
            
            if (!pattern.Match(name).Success)
            {
                try
                {
                    var dirInfo = await _fileService.MkdirAsync(_fileService.RetrieveAbsoluteFromSystemPath(name));
                    
                    ReturnMessage = "Successfully created directory: " + dirInfo?.Name;
                    Log.Information(ReturnMessage);
                    return RedirectToAction(nameof(Browse), new { path = returnUrl });
                }
                catch (Exception e)
                {
                    Log.Error(e, "StorageController#Create");
                    ReturnMessage = "Error: Couldn't create directory.";
                    return RedirectToAction(nameof(Browse), new { path = returnUrl, offset, count });
                }
            }

            ReturnMessage = "You cannot use non-alphabetic characters in directory names.";
            return RedirectToAction(nameof(Browse), new { path = returnUrl, offset, count });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string currentPath)
        {
            var offset = int.Parse(string.IsNullOrEmpty(Get("Offset")) ? "0" : Get("Offset"));
            var count = int.Parse(string.IsNullOrEmpty(Get("Count")) ? "0" : Get("Count"));
            var contentsList = GetContents(currentPath).ToList();
            var toBeDeletedItemsModel = new DeleteResourcesViewModel()
            {
                ReturnPath = currentPath
            };
            toBeDeletedItemsModel.ApplyPaging(offset, count);
            toBeDeletedItemsModel.FromFileInfoList(contentsList);

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
                    await _fileService.Delete(contents);

                    ReturnMessage = "Successfully deleted elements.";
                    Log.Information(ReturnMessage);
                    return RedirectToAction(nameof(Browse), new { path = returnPath, offset,  count});
                }
                catch
                { 
                    ReturnMessage = "Error: Couldn't delete resource.";
                    return RedirectToAction(nameof(Browse), new { path =  returnPath,  offset,  count});
                }
            }

            ReturnMessage = "Error: Nothing to be deleted.";
            return RedirectToAction(nameof(Browse), new { path = returnPath });
        }

        [HttpGet]
        [Authorize(Roles = "Admin, FileManagerUser")]
        [AutoValidateAntiforgeryToken]
        public IActionResult Rename(string n)
        {
            var name = _fileService.RetrieveAbsoluteFromSystemPath(n);
            ViewData["path"] = name;
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
                var isRenamed = _fileService.Move(Path.Combine(rfm.AbsoluteParentPath, rfm.OldName), Path.Combine(rfm.AbsoluteParentPath, rfm.NewName));
                if (!isRenamed)
                {
                    ReturnMessage = "Couldn't renamed to " + rfm.NewName;
                    return RedirectToAction(nameof(Browse),
                        new {path = rfm.ReturnUrl});
                }
                ReturnMessage = "Successfully renamed to " + rfm.NewName;

                return RedirectToAction(nameof(Browse), 
                    new { path = rfm.ReturnUrl });

            }
            ModelState.AddModelError(HttpContext.TraceIdentifier, "New name cannot be empty!");
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
                var isHidden = _fileService.HideFile(
                    systemPath
                );
                ReturnMessage = isHidden ? "Resource hidden." : "Couldn't be hidden.";
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
            Log.Debug("Redirecting to Browse...");
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
                var isHidden = _fileService.ShowFile(
                    systemPath
                );
                ReturnMessage = isHidden ? "Resource is visible now." : "Couldn't be set visible.";
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
            Log.Debug("Redirecting to Browse...");
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

        private IDirectoryContents GetContents(string systemPath)
        {
            return _fileService.GetDirectoryContents(systemPath);
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
