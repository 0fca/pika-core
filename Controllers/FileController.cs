
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
        private static string last = Constants.RootPath;
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
            lrmv.Contents = getContents(path);
            
            if(_signInManager.Context.User.IsInRole("Admin")){
                return RedirectToAction(nameof(AdminFileView));
            }

            return RedirectToAction(nameof(FileView));
        }
        
        public IActionResult FileView(){
            return View(lrmv);
        }

        private IDirectoryContents getContents(string path)
        {
            if(String.IsNullOrEmpty(path)){
                path = Constants.RootPath;
            }else if(path.StartsWith("..")){
                path = System.IO.Directory.GetParent(last) != null ? System.IO.Directory.GetParent(last).ToString() : "/";
            }
            last = path;
            return _fileProvider.GetDirectoryContents(path);
        }

        [Authorize(Roles="Admin")]
        public IActionResult AdminFileView(){
            return View(lrmv);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateUrlView(string entryName){
            try{
                StorageIndexRecord s = null;
                _storageIndexContext.index_storage.ToList().ForEach(record =>{
                    if(record.absolute_path.Equals(String.Concat(last,"/",entryName))){
                        s = record;
                    }
                });

                if(s == null){
                    s = new StorageIndexRecord();
                    s.absolute_path = String.Concat(last, "/", entryName);
                    s.urlhash = _generatorService.GenerateId(s.absolute_path);
                    var user = await _signInManager.UserManager.GetUserAsync(HttpContext.User);
                    s.user_id =  user != null ? await _signInManager.UserManager.GetEmailAsync(user) : "Anonymous";
                    s.expires = true;
                    s.expire_date = ComputeDateTime();
                    _storageIndexContext.Add(s);
                    await _storageIndexContext.SaveChangesAsync();
                }
                ViewData["urlhash"] = s.urlhash;
                ViewData["host"] = HttpContext.Request.Host;
                ViewData["protocol"] = HttpContext.Request.IsHttps ? "https" : "http";
                ViewData["returnUrl"] = "/File?path="+last;
                return View();
            }catch(InvalidOperationException ex){
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> PermanentDownload(string id){
            StorageIndexRecord s = null;
            try{
                s = _storageIndexContext.index_storage.Single(record => record.urlhash.Equals(id));
                var fileBytes = await _fileService.DownloadAsync(s.absolute_path);
                string name = Path.GetFileName(s.absolute_path);
                if(s.expires){
                    if(s.expire_date == DateTime.Now){
                        TempData["returnMessage"] = "Lilly thinks that this url expired today, you need to generate a new one.";
                        return RedirectToAction(nameof(Index), new {path = last});
                    }
                }
                return File(fileBytes, MIMEAssistant.GetMIMEType(name), name);
            }catch(InvalidOperationException ex){
                TempData["returnMessage"] = "The following error made Lilly stop working: "+ex.Message;
                return RedirectToAction(nameof(Index), new {path = last});
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<FileResult> Download(int id){
            var conts = lrmv.Contents.ToList();
            string path = "";
            path = conts.ElementAt(id).PhysicalPath;
            byte[] fileBytes = null;
            string name = conts.ElementAt(id).Name;
            string mime = "archive/zip";

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

        [HttpGet]
        [Authorize(Roles="Admin")]
        public IActionResult Delete(){
            return View(lrmv);
        }

        [HttpPost]
        [Authorize(Roles="Admin")]
        public IActionResult DeleteConfirmation(string path){
            string parent =  System.IO.Directory.GetParent(path).FullName;
            try{
                if(System.IO.File.Exists(path)) { 
                    System.IO.File.Delete(path);
                    return RedirectToAction(nameof(Index), new {path = parent});
                }else{ 
                    System.IO.Directory.Delete(path);
                    return RedirectToAction(nameof(Index), new {path = parent});
                }
            }catch(Exception e){
                Debug.WriteLine(e.StackTrace);
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorCode = HttpContext.Response.StatusCode });
            }
        }

        [HttpGet]
        [Authorize(Roles="Admin")]
        public IActionResult Rename(int rid){
            var name = lrmv.Contents.ElementAt(rid).PhysicalPath;
            ViewData["path"] = name;
            RenameFileModel rfm = new RenameFileModel();

            rfm.IsDirectory = IsDirectory(name);
            rfm.OldName = GetName(name);
            rfm.AbsolutePath = name;
            return View(rfm);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles="Admin")]
        public IActionResult Rename(RenameFileModel rfm){
            if(!String.IsNullOrEmpty(rfm.NewName)){
                //ViewData["type"] = rfm.IsDirectory ? "Directory" : "File";
            if(rfm.IsDirectory){
                System.IO.Directory.Move(rfm.AbsolutePath, System.IO.Directory.GetParent(rfm.AbsolutePath)+"/"+rfm.NewName);
                return RedirectToAction(nameof(Index), new {path = System.IO.Directory.GetParent(rfm.AbsolutePath)});
            }else{
                System.IO.File.Move(rfm.AbsolutePath, System.IO.Directory.GetParent(rfm.AbsolutePath)+"/"+rfm.NewName);
                return RedirectToAction(nameof(Index), new {path = System.IO.Directory.GetParent(rfm.AbsolutePath)});
            }
            }else{
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorCode = HttpContext.Response.StatusCode  });
            }
        }

        private bool IsDirectory(string name){
            return !System.IO.File.Exists(name);
        }

        private string GetName(string absolutePath){
            return IsDirectory(absolutePath) ? System.IO.Path.GetDirectoryName(absolutePath) : System.IO.Path.GetFileName(absolutePath);
        }

        private DateTime ComputeDateTime(){
            var now = DateTime.Now;
            now = now.AddDays(Constants.DayCount);
            return now;
        }
    }
}
