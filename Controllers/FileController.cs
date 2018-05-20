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
using System.Text;
using System.IO.Compression;
using FMS2.Services;
using Microsoft.Extensions.Logging;

namespace FMS2.Controllers
{
    [Route("[controller]/[action]")]
    public class FileController : Controller
    {
        private readonly IFileProvider _fileProvider;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IFilesystemInterface _archiveService;
        private readonly ILogger<FileController> _iLogger;
        private static FileResultModel lrmv = new FileResultModel();
        private static string last = Constants.RootPath;
        public FileController(IFileProvider fileProvider, SignInManager<ApplicationUser> signInManager, IFilesystemInterface archiveService, ILogger<FileController> iLogger)
        {
            _signInManager = signInManager;
            _fileProvider = fileProvider;
            _archiveService = archiveService;
            _iLogger = iLogger;
        }

        [AllowAnonymous]
        public IActionResult Index(string path)
        {
            lrmv.Contents = getContents(path);
            
            if(_signInManager.Context.User.IsInRole("Admin") || _signInManager.Context.User.IsInRole("FileManagerUser")){
                return RedirectToAction(nameof(AdminFileView));
            }

            return RedirectToAction(nameof(FileView));
        }
        
        public IActionResult FileView(){
            if(lrmv.Contents != null){
                return View(lrmv);
            }else{
                return RedirectToAction(nameof(Index), new {path = last});
            }
        }

        private IDirectoryContents getContents(string path)
        {
            if(String.IsNullOrEmpty(path)){
                path = Constants.RootPath;
            }else if(path.StartsWith("..")){
                path = System.IO.Directory.GetParent(last).FullName.Length > Constants.RootPath.Length ? System.IO.Directory.GetParent(last).ToString() : Constants.RootPath;
            }
            _iLogger.LogDebug(path);
            _iLogger.LogInformation(path);
            last = path;
            return _fileProvider.GetDirectoryContents(path);
        }

        [Authorize(Roles="Admin, FileManagerUser")]
        public IActionResult AdminFileView(){
            if(lrmv.Contents != null){
                return View(lrmv);
            }else{
                return RedirectToAction(nameof(Index), new {path = last});
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<FileContentResult> Download(int id){
            byte[] fileBytes = new byte[0];
            string path = "";
            var conts = lrmv.Contents.ToList();
            path = conts.ElementAt(id).PhysicalPath;
            fileBytes = await System.IO.File.ReadAllBytesAsync(path);
            return File(fileBytes, "text/plain", Path.GetFileName(path));
        }

        [HttpGet]
        [Authorize(Roles="Admin,FileManagerUser")]
        [AutoValidateAntiforgeryToken]
        public async Task<FileResult> DownloadDirectory(string name){
            string absolutePath = String.Concat(last,"/",name);
            string output = String.Concat(Constants.Tmp,"/",name+".zip");
            await _archiveService.ZipDirectoryAsync(absolutePath, output);
            Debug.WriteLine(absolutePath);
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(output);
            return File(fileBytes, "archive/zip" , Path.GetFileName(output));
        }

        [HttpGet]
        [Authorize(Roles="Admin, FileManagerUser")]
        [AutoValidateAntiforgeryToken]
        public IActionResult Delete(){
            return View(lrmv);
        }

        [HttpPost]
        [Authorize(Roles="Admin, FileManagerUser")]
        public IActionResult DeleteConfirmation(FileResultModel frm){
            //Debug.WriteLine(path);
            IEnumerator<IFileInfo> ei = frm.Contents.GetEnumerator();
            string parent = "";
            int countDeleted = 0;
            while(ei.MoveNext()){
                string path = frm.Contents.GetEnumerator().Current.PhysicalPath;

                if(!String.IsNullOrEmpty(path)){
                parent = System.IO.Directory.GetParent(path).FullName;
                try{
                    if(System.IO.File.Exists(path)) { 
                        System.IO.File.Delete(path);
                    }else{ 
                        Debug.WriteLine(path);
                        System.IO.Directory.Delete(path);
                    }
                    countDeleted++;
                }catch(Exception e){
                    Debug.WriteLine(e.StackTrace);
                    //return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
                }
                }else{
                    //return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
                }
            }
            TempData["deletedCount"] = countDeleted;
            return RedirectToAction(nameof(AdminFileView), new {path = parent});
        }

        [HttpPost]
        [Authorize(Roles="Admin, FileManagerUser")]
        [AutoValidateAntiforgeryToken]
        public IActionResult Create(string name){
            Debug.WriteLine(String.Concat(last,"/",name));
            if(!Directory.Exists(String.Concat(last,"/",name))){
                Directory.CreateDirectory(String.Concat(last,"/",name));
            }
            return RedirectToAction(nameof(AdminFileView));
        }

        [HttpGet]
        [Authorize(Roles="Admin, FileManagerUser")]
        [AutoValidateAntiforgeryToken]
        public IActionResult Rename(int rid){
            string name = "";
            var info = lrmv.Contents.ToList().ToArray()[rid];
            name = info.PhysicalPath;
            ViewData["path"] = name;
            ViewData["type"] = IsDirectory(name) ? "directory ": "file";
            RenameFileModel rfm = new RenameFileModel();
            rfm.IsDirectory = IsDirectory(name);
            rfm.OldName = GetName(name);
            rfm.AbsolutePath = name;
            return View(rfm);
        }

        [HttpPost]
        [Authorize(Roles="Admin, FileManagerUser")]
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
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        private bool IsDirectory(string name){
            return !System.IO.File.Exists(name);
        }

        private string GetName(string absolutePath){
            return IsDirectory(absolutePath) ? System.IO.Path.GetDirectoryName(absolutePath) : System.IO.Path.GetFileName(absolutePath);
        }
    }
}