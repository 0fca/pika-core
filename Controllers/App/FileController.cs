using Api.Hubs;
using FMS.Controllers.Helpers;
using FMS2.Data;
using FMS2.Models;
using FMS2.Models.File;
using FMS2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMS2.Controllers
{
    public class FileController : Controller
    {
        private readonly IFileProvider _fileProvider;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly static FileResultModel lrmv = new FileResultModel();
        private readonly IZipper _archiveService;
        private readonly IFileOperator _fileService;
        private readonly IGenerator _generatorService;
        private readonly ILogger<FileController> _iLogger;
        private readonly IFileLoggerService _loggerService;
        private readonly StorageIndexContext _storageIndexContext;
        private string _last = Constants.RootPath;
        private bool wasArchivingCancelled = true;
        private readonly IHubContext<StatusHub> _hubContext;
        private static readonly FormOptions _defaultFormOptions = new FormOptions();

        public FileController(IFileProvider fileProvider, 
        SignInManager<ApplicationUser> signInManager, 
        IZipper archiveService, IFileOperator fileService, 
        ILogger<FileController> iLogger, IGenerator iGenerator, 
        StorageIndexContext storageIndexContext, 
        IFileLoggerService fileLoggerService,
        IHubContext<StatusHub> hubContext)
        {
            _signInManager = signInManager;
            _fileProvider = fileProvider;
            _archiveService = archiveService;
            _fileService = fileService;
            _iLogger = iLogger;
            _generatorService = iGenerator;
            _storageIndexContext = storageIndexContext;
            _loggerService = fileLoggerService;
            _hubContext = hubContext;

            ((ArchiveService)_archiveService).PropertyChanged += new PropertyChangedEventHandler(PropertyChangedHandler);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string path)
        {
            var tmp = GetContents(path);
            if (tmp.Exists)
            {
                lrmv.Contents = await SortContents(tmp);
                if (null != lrmv.Contents)
                {
                    _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Got contents for " + _last);
                    return RedirectToAction(_signInManager.Context.User.IsInRole("Admin") ? nameof(BrowseAdmin) : nameof(Browse));
                }
                else
                {
                    return RedirectToAction("Error", "Home", new ErrorViewModel { ErrorCode = -1, Message = "There was an error while reading directory content.", RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
                }
            }
            else if (_fileProvider.GetFileInfo(path).Exists)
            {
                return RedirectToAction(nameof(Download), new { @id = _fileProvider.GetFileInfo(path).Name });
            }
            else
            {
                _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), _last+" does not exist on the filesystem.");
                TempData["returnMessage"] = "The resource doesn't exist on the filesystem.";
                return RedirectToAction(_signInManager.Context.User.IsInRole("Admin") ? nameof(BrowseAdmin) : nameof(Browse));
            }
        }

        private async Task<List<IFileInfo>> SortContents(IDirectoryContents tmp)
        {
            var asyncFileEnum = await Task.Factory.StartNew(() => tmp.Where(entry => !entry.IsDirectory).OrderBy(predicate => predicate.Name));
            var asyncDirEnum = await Task.Factory.StartNew(() => tmp.Where(entry => entry.IsDirectory).OrderBy(predicate => predicate.Name));
            var resultList = new List<IFileInfo>();
            resultList.AddRange(asyncDirEnum);
            resultList.AddRange(asyncFileEnum);
            return resultList;
        }

        [AllowAnonymous]
        public IActionResult Browse()
        {
            TempData["showDownloadPartial"] = true;
            if (_last != null) ViewData["returnUrl"] = UnixHelper.GetParent(GetLastPath()); ViewData["path"] = GetLastPath();
            return View(lrmv);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult BrowseAdmin()
        {
            TempData["showDownloadPartial"] = true;
            if (_last != null) ViewData["returnUrl"] = UnixHelper.GetParent(GetLastPath()); ViewData["path"] = GetLastPath();
            return View(lrmv);
        }

        private IDirectoryContents GetContents(string path)
        {
                _last = GetLastPath();
                if (string.IsNullOrEmpty(path))
                {
                    path = Constants.RootPath;
                }

                if (Path.IsPathRooted(path))
                {
                    _last = path;
                    HttpContext.Session.Set("lastPath", System.Text.Encoding.UTF8.GetBytes(_last));
                    return _fileProvider.GetDirectoryContents(_last);
                }

                if (path.Equals("/"))
                {
                    _last = Constants.RootPath;
                    HttpContext.Session.Set("lastPath", System.Text.Encoding.UTF8.GetBytes(_last));
                    return _fileProvider.GetDirectoryContents(_last);
                }
                if (!_last.Equals("/"))
                {
                    _last = string.Concat(_last, "/", path);
                }
                else
                {
                    _last = string.Concat(_last, path);
                }

                UnixHelper.ClearPath(ref _last);
                HttpContext.Session.Set("lastPath", System.Text.Encoding.UTF8.GetBytes(_last));
            return _fileProvider.GetDirectoryContents(_last);
        }


        [HttpGet]
        [AllowAnonymous]
        [Route("/[controller]/[action]/{name?}")]
        public async Task<IActionResult> GenerateUrlView(string name, string returnUrl)
        {
            var systemPart = GetLastPath().Equals("/")? GetLastPath()+name : (GetLastPath() + Path.DirectorySeparatorChar + name) ;
            string entryName = UnixHelper.MapToPhysical(Constants.FileSystemRoot, systemPart);

            if (!string.IsNullOrEmpty(entryName))
            {
                try
                {
                    StorageIndexRecord s = null;
                    _storageIndexContext.index_storage.ToList().ForEach(record =>
                    {
                        if (record.absolute_path.Equals(entryName))
                        {
                            s = record;
                        }
                    });

                    if (s == null)
                    {
                        s = new StorageIndexRecord { absolute_path = entryName };
                        s.urlhash = _generatorService.GenerateId(s.absolute_path);
                        var user = await _signInManager.UserManager.GetUserAsync(HttpContext.User);
                        s.user_id = user != null ? await _signInManager.UserManager.GetEmailAsync(user) : HttpContext.Connection.RemoteIpAddress.ToString();
                        s.expires = true;
                        s.expire_date = ComputeDateTime();
                        _storageIndexContext.Add(s);
                        await _storageIndexContext.SaveChangesAsync();
                    }
                    else
                    {
                        if (s.expire_date.Date == DateTime.Now.Date || s.expire_date.Date < DateTime.Now.Date)
                        {
                            s.expire_date = ComputeDateTime();
                            _storageIndexContext.Update(s);
                            await _storageIndexContext.SaveChangesAsync();
                        }
                        else
                        {
                            _loggerService.LogToFileAsync(LogLevel.Warning, HttpContext.Connection.RemoteIpAddress.ToString(), "Record for the file: " + entryName + " exists in the database, no need of updating it.");
                        }
                    }
                    ViewData["urlhash"] = s.urlhash;
                    ViewData["host"] = HttpContext.Request.Host;
                    ViewData["protocol"] = "https";
                    ViewData["returnUrl"] = returnUrl;
                    return View();
                }
                catch (InvalidOperationException ex)
                {
                    _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), ex.Message);
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> PermanentDownload(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                StorageIndexRecord s = null;
                try
                {
                    s = _storageIndexContext.index_storage.SingleOrDefault(record => record.urlhash.Equals(id));

                    if (s != null)
                    {
                        var fileBytes = await _fileService.DownloadAsStreamAsync(s.absolute_path);
                        var name = Path.GetFileName(s.absolute_path);
                        if (fileBytes != null)
                        {
                            if (!s.expires)
                            {
                                _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully returned requested resource" + s.absolute_path);
                                return File(fileBytes, MIMEAssistant.GetMIMEType(name), name);
                            }

                            if (s.expire_date != DateTime.Now && s.expire_date > DateTime.Now)
                            {
                                _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully returned requested resource" + s.absolute_path);
                                return File(fileBytes, MIMEAssistant.GetMIMEType(name), name);
                            }

                            TempData["returnMessage"] = "It seems that this url expired today, you need to generate a new one.";

                            return RedirectToAction(nameof(Index), new { path = _last });
                        }
                        else
                        {
                            _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Request.Host.Value, "Couldn't read requested resource: " + s.absolute_path);
                            TempData["returnMessage"] = "Couldn't read requested resource: " + s.urlid;
                            //return RedirectToAction("Error", "Home", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorCode = HttpContext.Response.StatusCode, Message = "This resource isn't accessible at the moment." });
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    TempData["returnMessage"] = "It seems that given token doesn't exist in the database.";
                    return RedirectToAction(nameof(Index), new { path = _last });
                }
                catch (InvalidOperationException ex)
                {
                    TempData["returnMessage"] = "Couldn't read requested resource: " + s.urlid;
                    _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), ex.Message);
                    return RedirectToAction(nameof(Index), new { path = _last });
                }
            }
            else
            {
                TempData["returnMessage"] = "No id given.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> Download(string id, bool z = false)
        {
            var name = id;
            if (!string.IsNullOrEmpty(name))
            {
                    var systemsAbsolute = GetLastPath();
                    var fileInfo = _fileProvider.GetFileInfo(string.Concat(systemsAbsolute, "/", name));
                    var path = fileInfo.PhysicalPath;
                    
                    if (!fileInfo.Exists)
                    {
                        ViewData["returnMessage"] = "File doesn't exist on server's filesystem.";
                        if (z)
                            path = string.Concat(Constants.Tmp + name);
                        else
                            return RedirectToAction(nameof(Index), new { @path = GetLastPath()});
                    }
                        var mime = "archive/zip";

                        if (System.IO.File.Exists(path))
                        {
                            mime = MIMEAssistant.GetMIMEType(name);
                            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 8192, true);
                            await _hubContext.Clients.All.SendAsync("DownloadStarted");
                            _loggerService.LogToFileAsync(LogLevel.Warning, HttpContext.Connection.RemoteIpAddress.ToString(), "Attempting to return file with name: " + name + " as an asynchronous stream.");
                            return File(fs, mime, name);
                        }
                        else
                        {
                            if (Directory.Exists(path))
                            {
                                TempData["returnMessage"] = "This is a folder, cannot download it directly.";
                                return RedirectToAction(nameof(Index), new { @path = GetLastPath() });
                            }
                            else
                            {
                                TempData["returnMessage"] = "The path " + path + " does not exist on server's filesystem.";
                                return RedirectToAction(nameof(Index));
                            }
                        }
                    }
                    else
                    {
                        _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Couldn't read resuested resource: " + name);
                        TempData["returnMessage"] = "Couldn't read requested resource: " + name;
                        return RedirectToAction(nameof(Index));
                    }
                
        }

        [HttpPost]
        [AllowAnonymous]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            files.RemoveAll(element => element.Length > Constants.MaxUploadSize);
            long size = files.Sum(f => f.Length);
            // full path to file in temp location
            var filePath = Constants.Tmp+Constants.UploadTmp;

            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    using (var stream = new FileStream(filePath+ Path.DirectorySeparatorChar+formFile.FileName, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                }
            }

            var uploadedFiles = Directory.GetFiles(Constants.Tmp+Constants.UploadTmp);
            foreach (var file in uploadedFiles) {
                await _fileService.MoveFromTmpAsync(Path.GetFileName(file), Constants.UploadDirectory);
            }

            TempData["returnMessage"] = files.Count+" files uploaded of summary size "+size+" "+UnixHelper.DetectUnitBySize(size);
            return RedirectToAction(nameof(Index), new { @path = GetLastPath()});
        }

        [HttpGet]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin,FileManagerUser")]
        public async Task<IActionResult> Archive(string id)
        {
            var systemsAbsolute = GetLastPath();
            var output = string.Concat(Constants.Tmp, id, ".zip");

            var path =_fileProvider.GetFileInfo(string.Concat(systemsAbsolute, "/", id)).PhysicalPath;

            if (!((ArchiveService)_archiveService).WasStartedAlready())
            {
                var task = await _archiveService.ZipDirectoryAsync(path, output);
                await _hubContext.Clients.All.SendAsync("ReceiveArchivingStatus", "Zipping task started...");
                await Task.WhenAll(task);
                
                    if (task.IsCompleted)
                    {
                        if (wasArchivingCancelled)
                        {
                            TempData["returnMessage"] = "Archiving was cancelled by user.";
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            return RedirectToAction(nameof(Download), new { @id = string.Concat(id, ".zip") , @z = true});
                        }
                    }
                    else
                    {
                        TempData["returnMessage"] = "Something unexpected happened.";
                        return RedirectToAction(nameof(Index));
                    }
            }
            else
            {
                TempData["returnMessage"] = "All signs on the Earth and on the sky say that you have already ordered Pika Cloud to zip something.";
                return RedirectToAction(nameof(Index));
            }
        }

        
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin, FileManagerUser")]
        [Route("/[controller]/[action]/{name?}")]
        public IActionResult Create(string name)
        {
            try
            {
                var dirInfo = Directory.CreateDirectory(string.Concat(_fileProvider.GetFileInfo(_last).PhysicalPath, Path.DirectorySeparatorChar, name));
                TempData["returnMessage"] = "Successfully created directory: " + dirInfo.Name;
                _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Created directory: " + dirInfo.FullName);
                return RedirectToAction(nameof(Index), new { path = _last });
            }
            catch (Exception e)
            {
                _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Couldn't create directory because of " + e.Message);
                TempData["returnMessage"] = "Error: Couldn't create directory.";
                return RedirectToAction(nameof(Index), new { path = _last });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete()
        {
            return View(lrmv);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmation(FileResultModel fileResultModel)
        {
            //var parent = fileResultModel.ReturnPath;
            var contents = fileResultModel.ToBeDeleted;
            if (contents.Count > 0)
            {
                try
                {
                    await contents.ToAsyncEnumerable().ForEachAsync(item => {
                            if (Directory.Exists(item))
                            {
                                Directory.Delete(item, true);
                            }
                            else
                            {
                                System.IO.File.Delete(item);
                            }
                    });

                    _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully deleted elements.");
                    TempData["returnMessage"] = "Successfully deleted elements.";
                    return RedirectToAction(nameof(Index), new { path = _last });
                }
                catch (Exception e)
                {
                    _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Couldn't delete resource because of " + e.Message);
                    TempData["returnMessage"] = "Error: Couldn't delete resource.";
                    return RedirectToAction(nameof(Index), new { path = _last });
                }
            }
            else
            {
                _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Cannot stat.");
                TempData["returnMessage"] = "Error: Nothing to be deleted.";
                return RedirectToAction(nameof(Index), new { path = _last });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin, FileManagerUser")]
        [AutoValidateAntiforgeryToken]
        [Route("/[controller]/[action]/{inname?}")]
        public IActionResult Rename(string inname)
        {
            string name = UnixHelper.MapToPhysical(Constants.FileSystemRoot, GetLastPath()+inname);
            ViewData["path"] = name;
            RenameFileModel rfm = new RenameFileModel
            {
                IsDirectory = IsDirectory(name),
                OldName = IsDirectory(name) ? Path.GetDirectoryName(name+"/") : Path.GetFileName(name),
                AbsolutePath = name
            };
            _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Viewing Rename view for " + name);

            return View(rfm);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin, FileManagerUser")]
        public IActionResult Rename(RenameFileModel rfm)
        {
            if (!string.IsNullOrEmpty(rfm.NewName))
            {
                //ViewData["type"] = rfm.IsDirectory ? "Directory" : "File";
                if (rfm.IsDirectory)
                {
                    System.IO.Directory.Move(rfm.AbsolutePath, System.IO.Directory.GetParent(rfm.AbsolutePath) + "/" + rfm.NewName);
                    _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully renamed directory " + rfm.OldName + " to " + rfm.NewName);
                    TempData["returnMessage"] = "Successfully renamed to " + rfm.NewName;
                    return RedirectToAction(nameof(Index), new { path = _last });
                }
                else
                {
                    System.IO.File.Move(rfm.AbsolutePath, System.IO.Directory.GetParent(rfm.AbsolutePath) + "/" + rfm.NewName);
                    _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully renamed file " + rfm.OldName + " to " + rfm.NewName);
                    TempData["returnMessage"] = "Successfully renamed to " + rfm.NewName;
                    return RedirectToAction(nameof(Index), new { path = _last });
                }

            }
            else
            {
                _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Rename action aborted because passed data were inappropiate.");
                ModelState.AddModelError(HttpContext.TraceIdentifier, "New name cannot be empty!");
                return View(rfm);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin, FileManagerUser")]
        [AutoValidateAntiforgeryToken]
        public IActionResult CancelDownloadAsync()
        {
            _archiveService.Cancel();
            _hubContext.Clients.All.SendAsync("ArchivingCancelled", "Cancelled by the user.");
            _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Attempting to cancel download task.");
            return RedirectToAction(nameof(Index));
        }
        #region HelperMethods

        public void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            wasArchivingCancelled = false;
        }

        private static bool IsDirectory(string name)
        {
            return !System.IO.File.Exists(name);
        }

        private static string GetName(string absolutePath)
        {
            return IsDirectory(absolutePath) ? System.IO.Path.GetDirectoryName(absolutePath) : System.IO.Path.GetFileName(absolutePath);
        }

        private static DateTime ComputeDateTime()
        {
            var now = DateTime.Now;
            now = now.AddDays(Constants.DayCount);
            return now;
        }

        private string GetLastPath()
        {
            HttpContext.Session.TryGetValue("lastPath", out byte[] result);
            return result != null ? System.Text.Encoding.UTF8.GetString(result) : "/";
        }

        private static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader = Microsoft.Net.Http.Headers.MediaTypeHeaderValue.TryParse(section.ContentType, out Microsoft.Net.Http.Headers.MediaTypeHeaderValue mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }
        #endregion
    }
}
