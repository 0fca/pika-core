
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FMS2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Identity;
using FMS2.Models.File;
using FMS2.Services;
using Microsoft.Extensions.Logging;
using FMS2.Data;
using FMS.Controllers.Helpers;

namespace FMS2.Controllers
{
    public class FileController : Controller
    {
        private readonly IFileProvider _fileProvider;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly static FileResultModel lrmv = new FileResultModel();
        private readonly IZipper _archiveService;
        private readonly IFileDownloader _fileService;
        private readonly IGenerator _generatorService;
        private readonly ILogger<FileController> _iLogger;
        private readonly IFileLoggerService _loggerService;
        private readonly StorageIndexContext _storageIndexContext;
        private static string _last = Constants.RootPath;

        public FileController(IFileProvider fileProvider, SignInManager<ApplicationUser> signInManager, IZipper archiveService, IFileDownloader fileService, ILogger<FileController> iLogger, IGenerator iGenerator, StorageIndexContext storageIndexContext, IFileLoggerService fileLoggerService)
        {
            _signInManager = signInManager;
            _fileProvider = fileProvider;
            _archiveService = archiveService;
            _fileService = fileService;
            _iLogger = iLogger;
            _generatorService = iGenerator;
            _storageIndexContext = storageIndexContext;
            _loggerService = fileLoggerService;
        }

        [AllowAnonymous]
        public IActionResult Index(string path)
        {
            lrmv.Contents = GetContents(path);

            if (typeof(NotFoundDirectoryContents) != lrmv.Contents.GetType()) {
                return RedirectToAction(_signInManager.Context.User.IsInRole("Admin") ? nameof(AdminFileView) : nameof(FileView));
            }
            else
            {
                return RedirectToAction("Error", "Home", new ErrorViewModel { ErrorCode = -1, Message="This directory doesn't exist on the server's filesystem.", RequestId=Activity.Current.Id});
            }
        }
        
        public IActionResult FileView(){
            if (_last != null) ViewData["parent"] = UnixHelper.GetParent(_last); ViewData["path"] = _last;
            return View(lrmv);
        }

        private IDirectoryContents GetContents(string path)
        {
            if(string.IsNullOrEmpty(path)){
                path = Constants.RootPath;
            }

            if(Path.IsPathRooted(path)){
                _last = path;
                return _fileProvider.GetDirectoryContents(_last);
            }

            if(path.Equals("/")){
                _last = Constants.RootPath;
                return _fileProvider.GetDirectoryContents(_last);
            }
            if(!_last.Equals("/")){
                _last = string.Concat(_last,"/",path);
            }else{
                _last = string.Concat(_last,path);
            }
            UnixHelper.ClearPath(ref _last);
            _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(),"Got contents for "+_last);
            return _fileProvider.GetDirectoryContents(_last);
        }

        [Authorize(Roles="Admin")]
        public IActionResult AdminFileView(){
            if (_last != null) ViewData["parent"] = UnixHelper.GetParent(_last); ViewData["path"] = _last;
            return View(lrmv);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateUrlView(string entryName){
            if (!String.IsNullOrEmpty(entryName))
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
                        _loggerService.LogToFileAsync(LogLevel.Warning, HttpContext.Connection.RemoteIpAddress.ToString(), "Record for the file: " + entryName + " exists in the database.");
                    }
                    ViewData["urlhash"] = s.urlhash;
                    ViewData["host"] = HttpContext.Request.Host;
                    ViewData["protocol"] = "https";
                    ViewData["returnUrl"] = "/File?path=" + _last;
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
        public async Task<ActionResult> PermanentDownload(string id){
            if (!String.IsNullOrEmpty(id))
            {
                StorageIndexRecord s = null;
                try
                {
                    s = _storageIndexContext.index_storage.SingleOrDefault(record => record.urlhash.Equals(id));

                    if (s != null)
                    {
                        var fileBytes = await _fileService.DownloadAsync(s.absolute_path);
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

                            TempData["returnMessage"] = "FMS thinks that this url expired today, you need to generate a new one.";

                            return RedirectToAction(nameof(Index), new { path = _last });
                        }
                        else
                        {
                            _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Request.Host.Value, "Couldn't read requested resource: " + s.absolute_path);
                            return RedirectToAction("Error", "Home", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorCode = HttpContext.Response.StatusCode, Message = "This resource isn't accessible at the moment." });
                        }
                    }
                    TempData["returnMessage"] = "It seems that given token doesn't exist in the database.";
                    return RedirectToAction(nameof(Index), new { path = _last });
                }
                catch (InvalidOperationException ex)
                {
                    TempData["returnMessage"] = "The following error made FMS stop working: " + ex.Message;
                    _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), ex.Message);
                    return RedirectToAction(nameof(Index), new { path = _last });
                }
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> Download(int id){
            if (lrmv.Contents != null && lrmv.Contents.ElementAt(id) != null)
            {
                var conts = lrmv.Contents.ToList();
                var path = "";
                path = conts.ElementAt(id).PhysicalPath;
                byte[] fileBytes = null;
                var name = conts.ElementAt(id).Name;
                var mime = "archive/zip";

                if(System.IO.File.Exists(path)){
                    fileBytes = await _fileService.DownloadAsync(path);
                    mime = MIMEAssistant.GetMIMEType(name);
                }else{
                    var output = String.Concat(Constants.Tmp, name, ".zip");
                    await _archiveService.ZipDirectoryAsync(path, output);
                    fileBytes = await _fileService.DownloadAsync(output);
                    name = String.Concat(name,".zip");
                }
                _loggerService.LogToFileAsync(LogLevel.Warning, HttpContext.Connection.RemoteIpAddress.ToString(), "Attempting to return file of id " +id+" with name: "+name);
                return File(fileBytes, mime, name);
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles ="Admin")]
        public IActionResult Create(string name) {
            try
            { 
               
                var dirInfo = Directory.CreateDirectory(string.Concat(_fileProvider.GetFileInfo(_last).PhysicalPath, Path.DirectorySeparatorChar, name));
                TempData["returnMessage"] = "Successfully created directory: " + dirInfo.Name;
                _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Created directory: " +dirInfo.FullName);
                return RedirectToAction(nameof(Index), new { path = _last});
            }
            catch (Exception e)
            {
                _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Couldn't create directory because of " +e.Message);
                TempData["returnMessage"] = "Error: Couldn't create directory.";
                return RedirectToAction(nameof(Index), new { path = _last });
            }
        }

        [HttpGet]
        [Authorize(Roles ="Admin")]
        public IActionResult Delete() {
            return View(lrmv);
        }

        [HttpPost]
        [Authorize(Roles="Admin")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> DeleteConfirmation(FileResultModel fileResultModel){
            //var parent = fileResultModel.ReturnPath;
            var contents = fileResultModel.Contents;
            try{
                await contents.ToAsyncEnumerable().ForEachAsync(item => {
                    if (item.Exists)
                    {
                        if (item.IsDirectory)
                        {
                            Directory.Delete(item.PhysicalPath);
                        }
                        else
                        {
                            System.IO.File.Delete(item.PhysicalPath);
                        }
                    }
                });

                _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully deleted elements.");
                TempData["returnMessage"] = "Successfully deleted elements.";
                return RedirectToAction(nameof(Index),  new { path = _last });
            }catch(Exception e){
                _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Couldn't delete resource because of " + e.Message);
                TempData["returnMessage"] = "Error: Couldn't delete resource.";
                return RedirectToAction(nameof(Index), new { path = _last });
            }
        }

        [HttpGet]
        [Authorize(Roles="Admin")]
        public IActionResult Rename(int rid){
            var name = lrmv.Contents.ElementAt(rid).PhysicalPath;
            ViewData["path"] = name;
            RenameFileModel rfm = new RenameFileModel
            {
                IsDirectory = IsDirectory(name), OldName = name, AbsolutePath = name
            };
            _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Viewing Rename view for " +name);
            
            return View(rfm);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles="Admin")]
        public IActionResult Rename(RenameFileModel rfm){
            if(!string.IsNullOrEmpty(rfm.NewName)){
                //ViewData["type"] = rfm.IsDirectory ? "Directory" : "File";
            if(rfm.IsDirectory){
                System.IO.Directory.Move(rfm.AbsolutePath, System.IO.Directory.GetParent(rfm.AbsolutePath)+"/"+rfm.NewName);
                    _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully renamed directory " +rfm.OldName+" to "+rfm.NewName);
                    TempData["returnMessage"] = "Successfully renamed to "+rfm.NewName;
                    return RedirectToAction(nameof(Index), new {path = _last});
            }else{
                System.IO.File.Move(rfm.AbsolutePath, System.IO.Directory.GetParent(rfm.AbsolutePath)+"/"+rfm.NewName);
                    _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Successfully renamed file " + rfm.OldName + " to " + rfm.NewName);
                    TempData["returnMessage"] = "Successfully renamed to "+rfm.NewName;
                    return RedirectToAction(nameof(Index), new {path = _last});
            }

            }else{
                _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), "Rename action aborted because passed data were inappropiate.");
                return RedirectToAction("Error", "Home", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorCode = HttpContext.Response.StatusCode, Message = "Given data set is invalid. Please, try again." });
            }
        }

        [HttpPost]
        [Authorize(Roles="Admin")]
        [AutoValidateAntiforgeryToken]
        public IActionResult CancelDownloadAsync(){
            _archiveService.Cancel();
            _loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Connection.RemoteIpAddress.ToString(), "Attempting to cancel download task.");
            return RedirectToAction(nameof(Index));
        }

        private static bool IsDirectory(string name){
            return !System.IO.File.Exists(name);
        }

        private static string GetName(string absolutePath){
            return IsDirectory(absolutePath) ? System.IO.Path.GetDirectoryName(absolutePath) : System.IO.Path.GetFileName(absolutePath);
        }

        private static DateTime ComputeDateTime(){
            var now = DateTime.Now;
            now = now.AddDays(Constants.DayCount);
            return now;
        }
    }
}
