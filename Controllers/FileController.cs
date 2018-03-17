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
        public FileController(IFileProvider fileProvider, SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
            _fileProvider = fileProvider;
        }

        //[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Index(string path)
        {
            
            if(String.IsNullOrEmpty(path)){
                path = Constants.RootPath;
            }
            var contents = _fileProvider.GetDirectoryContents(path);
            FileResultModel lrmv = new FileResultModel();
            lrmv.Contents = contents;
            return View(lrmv);
           
        }
    }
}