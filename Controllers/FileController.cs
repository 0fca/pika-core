
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
        private readonly StorageIndexContext _storageIndexContext;
        private static string _last = Constants.RootPath;

        public FileController(IFileProvider fileProvider, SignInManager<ApplicationUser> signInManager, IZipper archiveService, IFileDownloader fileService, ILogger<FileController> iLogger, IGenerator iGenerator, StorageIndexContext storageIndexContext)
        {
            _signInManager = signInManager;
            _fileProvider = fileProvider;
            _archiveService = archiveService;
            _fileService = fileService;
            _iLogger = iLogger;
            _generatorService = iGenerator;
            _storageIndexContext = storageIndexContext;
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
                return RedirectToAction("Error","Home");
            }
        }
        
        public IActionResult FileView(){
            if (_last != null) ViewData["parent"] = UnixHelper.GetParent(_last); ViewData["path"] = _last;
            return View(lrmv);
        }

        private IDirectoryContents GetContents(string path)
        {
            if(String.IsNullOrEmpty(path)){
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
                _last = String.Concat(_last,"/",path);
            }else{
                _last = String.Concat(_last,path);
            }
            UnixHelper.ClearPath(ref _last);
            
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
            try{
                StorageIndexRecord s = null;
                _storageIndexContext.index_storage.ToList().ForEach(record =>{
                    if(record.absolute_path.Equals(entryName)){
                        s = record;
                    }
                });

                if(s == null){
                    s = new StorageIndexRecord {absolute_path = entryName};
                    s.urlhash = _generatorService.GenerateId(s.absolute_path);
                    var user = await _signInManager.UserManager.GetUserAsync(HttpContext.User);
                    s.user_id =  user != null ? await _signInManager.UserManager.GetEmailAsync(user) : HttpContext.Connection.RemoteIpAddress.ToString();
                    s.expires = true;
                    s.expire_date = ComputeDateTime();
                    _storageIndexContext.Add(s);
                    await _storageIndexContext.SaveChangesAsync();
                }
                ViewData["urlhash"] = s.urlhash;
                ViewData["host"] = HttpContext.Request.Host;
                ViewData["protocol"] = "https";
                ViewData["returnUrl"] = "/File?path="+_last;
                return View();
            }catch(InvalidOperationException ex){
                Console.WriteLine(ex.Message+" : "+String.Concat(_last,"/",entryName));
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> PermanentDownload(string id){
            StorageIndexRecord s = null;
            try{
                s = _storageIndexContext.index_storage.SingleOrDefault(record => record.urlhash.Equals(id));

                if(s != null){
                var fileBytes = await _fileService.DownloadAsync(s.absolute_path);
                var name = Path.GetFileName(s.absolute_path);
                if(fileBytes != null){
                    if (!s.expires) return File(fileBytes, MIMEAssistant.GetMIMEType(name), name);

                    if (s.expire_date != DateTime.Now && s.expire_date > DateTime.Now)
                        return File(fileBytes, MIMEAssistant.GetMIMEType(name), name);

                    TempData["returnMessage"] = "FMS thinks that this url expired today, you need to generate a new one.";

                    return RedirectToAction(nameof(Index), new {path = _last});
                }else{
                    return RedirectToAction("Error", "Home", new ErrorViewModel{RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorCode = HttpContext.Response.StatusCode , Message = "This resource isn't accessible at the moment."});
                }
                }
                TempData["returnMessage"] = "It seems that given token doesn't exist in the database.";
                return RedirectToAction(nameof(Index), new {path = _last});
            }catch(InvalidOperationException ex){
                TempData["returnMessage"] = "The following error made FMS stop working: "+ex.Message;
                return RedirectToAction(nameof(Index), new {path = _last});
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<FileResult> Download(int id){
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
            
            return File(fileBytes, mime, name);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="Admin")]
        public IActionResult Create(string parent, string name) {
            try
            {
                var dirInfo = Directory.CreateDirectory(String.Concat(parent,"/",name));
                TempData["returnMessage"] = "Successfully created directory: " + dirInfo.FullName;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception e)
            {
                return RedirectToAction("Error", "Home", new ErrorViewModel { RequestId = Activity.Current.Id, Message = e.Message, ErrorCode = HttpContext.Response.StatusCode});
            }
        }

        [HttpGet]
        [Authorize(Roles="Admin")]
        public IActionResult Delete(){
            return View(lrmv);
        }

        [HttpPost]
        [Authorize(Roles="Admin")]
        public IActionResult DeleteConfirmation(string path){
            var parent =  System.IO.Directory.GetParent(path).FullName;
            try{
                if(System.IO.File.Exists(path)) { 
                    System.IO.File.Delete(path);
                    return RedirectToAction(nameof(Index), new {path = parent});
                }else{ 
                    System.IO.Directory.Delete(path);
                    return RedirectToAction(nameof(Index), new {path = parent});
                }
            }catch(Exception e){
                return RedirectToAction("Error", "Home", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorCode = HttpContext.Response.StatusCode, Message = e.Message});
            }
        }

        [HttpGet]
        [Authorize(Roles="Admin")]
        public IActionResult Rename(int rid){
            var name = lrmv.Contents.ElementAt(rid).PhysicalPath;
            ViewData["path"] = name;
            RenameFileModel rfm = new RenameFileModel
            {
                IsDirectory = IsDirectory(name), OldName = GetName(name), AbsolutePath = name
            };

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
                return RedirectToAction(nameof(Index), new {path = System.IO.Directory.GetParent(rfm.AbsolutePath)});
            }else{
                System.IO.File.Move(rfm.AbsolutePath, System.IO.Directory.GetParent(rfm.AbsolutePath)+"/"+rfm.NewName);
                return RedirectToAction(nameof(Index), new {path = System.IO.Directory.GetParent(rfm.AbsolutePath)});
            }
            }else{
                return RedirectToAction("Error", "Home", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorCode = HttpContext.Response.StatusCode, Message = "Given data set is invalid. Please, try again." });
            }
        }

        [HttpPost]
        [Authorize(Roles="Admin")]
        [AutoValidateAntiforgeryToken]
        public IActionResult CancelDownloadAsync(){
            _archiveService.Cancel();
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
