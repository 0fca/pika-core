using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FMS2.Controllers.Api.Hubs;
using FMS2.Controllers.Helpers;
using FMS2.Data;
using FMS2.Models;
using FMS2.Models.File;
using FMS2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace FMS2.Controllers.App
{
    public class FileController : Controller
    {
        private readonly IFileProvider _fileProvider;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private static readonly FileResultModel Lrmv = new FileResultModel();
        private readonly IZipper _archiveService;
        private readonly IFileOperator _fileService;
        private readonly IGenerator _generatorService;
        private readonly IFileLoggerService _loggerService;
        private readonly IConfiguration _configuration;
        private readonly StorageIndexContext _storageIndexContext;
        private string _last = Constants.RootPath;
        private bool _wasArchivingCancelled = true;
        private readonly IHubContext<StatusHub> _hubContext;
        private readonly IHubContext<FileOperationHub> _fileOperationHub;

        public FileController(IFileProvider fileProvider, 
        SignInManager<ApplicationUser> signInManager, 
        IZipper archiveService, IFileOperator fileService, 
        ILogger<FileController> iLogger, IGenerator iGenerator, 
        StorageIndexContext storageIndexContext, 
        IFileLoggerService fileLoggerService,
        IHubContext<StatusHub> hubContext,
        IHubContext<FileOperationHub> fileOperationHub,
        IConfiguration configuration)
        {
            _signInManager = signInManager;
            _fileProvider = fileProvider;
            _archiveService = archiveService;
            _fileService = fileService;
            _generatorService = iGenerator;
            _storageIndexContext = storageIndexContext;
            _loggerService = fileLoggerService;
            _hubContext = hubContext;
            _fileOperationHub = fileOperationHub;
            _configuration = configuration;

            ((ArchiveService)_archiveService).PropertyChanged += PropertyChangedHandler;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string path)
        {
            var tmp = GetContents(path);
            if (tmp.Exists)
            {
                Lrmv.Contents = await SortContents(tmp);
                var osUser = _configuration.GetSection("OsUser")["OsUsername"];
                if (!HttpContext.User.IsInRole("Admin"))
                {
                    Lrmv.Contents.RemoveAll(entry => UnixHelper.HasAccess(osUser, entry.PhysicalPath));
                }

                if (null == Lrmv.Contents)
                    return RedirectToAction("Error", "Home",
                        new ErrorViewModel
                        {
                            ErrorCode = -1, Message = "There was an error while reading directory content.",
                            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                        });
                
                _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Got contents for " + _last);
                return RedirectToAction(_signInManager.Context.User.IsInRole("Admin") ? nameof(BrowseAdmin) : nameof(Browse));

            }

            if (_fileProvider.GetFileInfo(path).Exists)
            {
                return RedirectToAction(nameof(Download), new { @id = _fileProvider.GetFileInfo(path).Name });
            }

            _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), _last+" does not exist on the filesystem.");
            TempData["returnMessage"] = "The resource doesn't exist on the filesystem.";
            return RedirectToAction(_signInManager.Context.User.IsInRole("Admin") ? nameof(BrowseAdmin) : nameof(Browse));
        }

        private static async Task<List<IFileInfo>> SortContents(IDirectoryContents tmp)
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
            return View(Lrmv);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult BrowseAdmin()
        {
            TempData["showDownloadPartial"] = true;
            if (_last != null) ViewData["returnUrl"] = UnixHelper.GetParent(GetLastPath()); ViewData["path"] = GetLastPath();
            return View(Lrmv);
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
                    HttpContext.Session.Set("lastPath", Encoding.UTF8.GetBytes(_last));
                    return _fileProvider.GetDirectoryContents(_last);
                }

                if (path.Equals("/"))
                {
                    _last = Constants.RootPath;
                    HttpContext.Session.Set("lastPath", Encoding.UTF8.GetBytes(_last));
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
                HttpContext.Session.Set("lastPath", Encoding.UTF8.GetBytes(_last));

            return _fileProvider.GetDirectoryContents(_last);
        }


        [HttpGet]
        [AllowAnonymous]
        [Route("/[controller]/[action]/{name?}")]
        public async Task<IActionResult> GenerateUrl(string name, string returnUrl)
        {
            var systemPart = GetLastPath().Equals("/")? GetLastPath()+name : (GetLastPath() + Path.DirectorySeparatorChar + name) ;
            var entryName = UnixHelper.MapToPhysical(Constants.FileSystemRoot, systemPart);

            var message = "No proper connection to database server.";
            
            if (ValidateDbServerState() && !string.IsNullOrEmpty(entryName))
            {
                try
                {
                    StorageIndexRecord s = null;
                    _storageIndexContext.IndexStorage.ToList().ForEach(record =>
                    {
                        if (record.AbsolutePath.Equals(entryName))
                        {
                            s = record;
                        }
                    });

                    if (s == null)
                    {
                        s = new StorageIndexRecord { AbsolutePath = entryName };
                        if (s != null)
                        {
                            s.Urlhash = _generatorService.GenerateId(s.AbsolutePath);
                            var user = await _signInManager.UserManager.GetUserAsync(HttpContext.User);
                            s.UserId = user != null
                                ? await _signInManager.UserManager.GetEmailAsync(user)
                                : HttpContext.Connection.RemoteIpAddress.ToString();
                            s.Expires = true;
                            s.ExpireDate = ComputeDateTime();
                            _storageIndexContext.Add(s);
                        }

                        await _storageIndexContext.SaveChangesAsync();
                    }
                    else
                    {
                        if (s.ExpireDate.Date == DateTime.Now.Date || s.ExpireDate.Date < DateTime.Now.Date)
                        {
                            s.ExpireDate = ComputeDateTime();
                            _storageIndexContext.Update(s);
                            await _storageIndexContext.SaveChangesAsync();
                        }
                        else
                        {
                            _loggerService.LogToFileAsync(LogLevel.Warning, HttpContext.Connection.RemoteIpAddress.ToString(), "Record for the file: " + entryName + " exists in the database, no need of updating it.");
                        }
                    }

                    if (s != null) TempData["urlhash"] = s.Urlhash;
                    TempData["url_name"] = name;
                    var port = HttpContext.Request.Host.Port;
                    TempData["host"] = HttpContext.Request.Host.Host + (port != null ? ":"+HttpContext.Request.Host.Port : "");
                    TempData["protocol"] = "https";
                    //Data["returnUrl"] = returnUrl;
                    return RedirectToAction(nameof(Index), new {@path = GetLastPath()});
                }
                catch (InvalidOperationException ex)
                {
                    _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), ex.Message);
                    message = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            if (ValidateDbHostState() && !ValidateDbServerState())
            {
                _storageIndexContext.Database.OpenConnection();
            }

            TempData["returnMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> PermanentDownload(string id)
        {
            if (ValidateDbServerState() && !string.IsNullOrEmpty(id))
            {
                StorageIndexRecord s = null;
                try
                {
                    s = _storageIndexContext.IndexStorage.SingleOrDefault(record => record.Urlhash.Equals(id));

                    if (s != null)
                    {
                        var fileBytes = await _fileService.DownloadAsStreamAsync(s.AbsolutePath);
                        var name = Path.GetFileName(s.AbsolutePath);
                        if (fileBytes != null)
                        {
                            if (!s.Expires)
                            {
                                _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully returned requested resource" + s.AbsolutePath);
                                return File(fileBytes, MimeAssistant.GetMimeType(name), name);
                            }

                            if (s.ExpireDate != DateTime.Now && s.ExpireDate > DateTime.Now)
                            {
                                _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully returned requested resource" + s.AbsolutePath);
                                return File(fileBytes, MimeAssistant.GetMimeType(name), name);
                            }

                            TempData["returnMessage"] = "It seems that this url expired today, you need to generate a new one.";

                            return RedirectToAction(nameof(Index), new { path = _last });
                        }
                        else
                        {
                            _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Request.Host.Value, "Couldn't read requested resource: " + s.AbsolutePath);
                            TempData["returnMessage"] = "Couldn't read requested resource: " + s.Urlid;
                            //return RedirectToAction("Error", "Home", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorCode = HttpContext.Response.StatusCode, Message = "This resource isn't accessible at the moment." });
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    TempData["returnMessage"] = "It seems that given token doesn't exist in the database.";
                    return RedirectToAction(nameof(Index), new { path = _last });
                }
                catch (InvalidOperationException ex)
                {
                    TempData["returnMessage"] = s != null ? "Couldn't read requested resource: " + s.Urlid : "Database error occured.";
                    _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), ex.Message);
                    return RedirectToAction(nameof(Index), new { path = _last });
                }
            }
            
            if (ValidateDbHostState() && !ValidateDbServerState())
            {
                _storageIndexContext.Database.OpenConnection();
               
            }
            
            TempData["returnMessage"] = "No id given.";
            return RedirectToAction(nameof(Index));
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
                            path = string.Concat(Constants.Tmp,name);
                        else
                            return RedirectToAction(nameof(Index), new { @path = GetLastPath()});
                    }

                    if (System.IO.File.Exists(path))
                        {
                            var mime = MimeAssistant.GetMimeType(name);
                            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 8192, true);
                            await _hubContext.Clients.All.SendAsync("DownloadStarted");
                            _loggerService.LogToFileAsync(LogLevel.Warning, HttpContext.Connection.RemoteIpAddress.ToString(), "Attempting to return file with name: " + name + " as an asynchronous stream.");
                            return File(fs, mime, name);
                        }

                if (Directory.Exists(path))
                {
                    TempData["returnMessage"] = "This is a folder, cannot download it directly.";
                    return RedirectToAction(nameof(Index), new { @path = GetLastPath() });
                }

                TempData["returnMessage"] = "The path " + path + " does not exist on server's filesystem.";
                return RedirectToAction(nameof(Index));
            }

            _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Couldn't read resuested resource: " + name);
            TempData["returnMessage"] = "Couldn't read requested resource: " + name;
            return RedirectToAction(nameof(Index));

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

            TempData["returnMessage"] = files.Count+" files uploaded of summary size "+size+" "+FileSystemAccessor.DetectUnitBySize(size);
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
                        if (!_wasArchivingCancelled)
                            return RedirectToAction(nameof(Download), new {@id = string.Concat(id, ".zip"), @z = true});
                        TempData["returnMessage"] = "Archiving was cancelled by user.";
                        return RedirectToAction(nameof(Index));

                    }

                TempData["returnMessage"] = "Something unexpected happened.";
                return RedirectToAction(nameof(Index));
            }

            TempData["returnMessage"] = "All signs on the Earth and on the sky say that you have already ordered Pika Cloud to zip something.";
            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin, FileManagerUser")]
        [Route("/[controller]/[action]/{name?}")]
        public IActionResult Create(string name)
        {
            var pattern = new Regex(@"\W|_");
            if (!pattern.Match(name).Success)
            {
                try
                {
                    var dirInfo = Directory.CreateDirectory(string.Concat(_fileProvider.GetFileInfo(_last).PhysicalPath,
                        Path.DirectorySeparatorChar, name));
                    TempData["returnMessage"] = "Successfully created directory: " + dirInfo.Name;
                    _loggerService.LogToFileAsync(LogLevel.Information,
                        HttpContext.Connection.RemoteIpAddress.ToString(), "Created directory: " + dirInfo.FullName);
                    return RedirectToAction(nameof(Index), new {path = _last});
                }
                catch (Exception e)
                {
                    _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(),
                        "Couldn't create directory because of " + e.Message);
                    TempData["returnMessage"] = "Error: Couldn't create directory.";
                    return RedirectToAction(nameof(Index), new {path = _last});
                }
            }

            TempData["returnMessage"] = "You cannot use non-alphabetic characters in directory names.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete()
        {
            return View(Lrmv);
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
        [Route("/[controller]/[action]/{inname}")]
        public IActionResult Rename(string inname)
        {
            var name = UnixHelper.MapToPhysical(Constants.FileSystemRoot, GetLastPath()+inname);
            ViewData["path"] = name;
            var rfm = new RenameFileModel
            {
                IsDirectory = IsDirectory(name),
                OldName = IsDirectory(name) ? Path.GetDirectoryName(name+"/") : Path.GetFullPath(name),
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
                    Directory.Move(rfm.AbsolutePath, Directory.GetParent(rfm.AbsolutePath) + "/" + rfm.NewName);
                    _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully renamed directory " + rfm.OldName + " to " + rfm.NewName);
                    TempData["returnMessage"] = "Successfully renamed to " + rfm.NewName;
                    return RedirectToAction(nameof(Index), new { path = _last });
                }

                System.IO.File.Move(rfm.AbsolutePath, Directory.GetParent(rfm.AbsolutePath) + "/" + rfm.NewName);
                _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully renamed file " + rfm.OldName + " to " + rfm.NewName);
                TempData["returnMessage"] = "Successfully renamed to " + rfm.NewName;
                return RedirectToAction(nameof(Index), new { path = _last });

            }

            _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Rename action aborted because passed data were inappropiate.");
            ModelState.AddModelError(HttpContext.TraceIdentifier, "New name cannot be empty!");
            return View(rfm);
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
            _wasArchivingCancelled = false;
        }

        private static bool IsDirectory(string name)
        {
            return !System.IO.File.Exists(name);
        }

        private static DateTime ComputeDateTime()
        {
            var now = DateTime.Now;
            now = now.AddDays(Constants.DayCount);
            return now;
        }

        private string GetLastPath()
        {
            HttpContext.Session.TryGetValue("lastPath", out var result);
            var outPath = result != null ? System.Text.Encoding.UTF8.GetString(result) : null;

            if (outPath != null && !outPath.EndsWith("/")) outPath += "/";
            
            return outPath ?? "/";
        }

        private bool ValidateDbHostState()
        {
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                var reply = pinger.Send(_configuration.GetSection("Network")["DbServer"]);
                if (reply != null) return reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                return false;
            }
            finally
            {
                pinger?.Dispose();
            }
            
            return false;
        }

        private bool ValidateDbServerState()
        {
            return (_storageIndexContext.Database.GetDbConnection() != null);
        }
        #endregion
    }
}
