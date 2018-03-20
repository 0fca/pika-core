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

namespace FMS2.Controllers
{
    public class FileController : Controller
    {
        private readonly IFileProvider _fileProvider;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly static FileResultModel lrmv = new FileResultModel();
        public FileController(IFileProvider fileProvider, SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
            _fileProvider = fileProvider;
        }

        //[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Index(string path)
        {
            lrmv.Contents = getContents(path);

            if(_signInManager.Context.User.IsInRole("Admin")){
                return RedirectToAction(nameof(AdminFileView));
            }
            return View(lrmv);
        }

        private IDirectoryContents getContents(string path)
        {
            if(String.IsNullOrEmpty(path)){
                path = Constants.RootPath;
            }
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
    }
}