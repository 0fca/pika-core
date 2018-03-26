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

namespace FMS2.Controllers
{
    public class FileController : Controller
    {
        private readonly IFileProvider _fileProvider;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly static FileResultModel lrmv = new FileResultModel();
        private static string last = "/";
        public FileController(IFileProvider fileProvider, SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
            _fileProvider = fileProvider;
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
        public async Task<FileResult> Download(int id){
            var conts = lrmv.Contents.ToList();
            string path = "";
            path = conts.ElementAt(id).PhysicalPath;
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(path);
            return File(fileBytes, "text/plain", Path.GetFileName(path));
        }

        [Authorize(Roles="Admin")]
        public IActionResult Delete(String name){
            string name1 = GetName(name);
            Debug.WriteLine("Name: "+System.IO.Path.GetDirectoryName(name)+" : "+name);
            ViewData["path"] = name;
            ViewData["name1"] = name;
            ViewData["type"] = IsDirectory(name) ? "File" : "Directory";
            return View();
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
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [HttpGet]
        [Authorize(Roles="Admin")]
        public IActionResult Rename(string name){
            ViewData["path"] = name;
            RenameFileModel rfm = new RenameFileModel();
            //ViewData["absol"] = name;
            rfm.IsDirectory = IsDirectory(name);
            rfm.OldName = GetName(name);
            rfm.AbsolutePath = name;
            //ViewData["OldName"] = rfm.OldName;
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