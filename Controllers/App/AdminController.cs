using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PikaCore.Models;
using PikaCore.Models.ManageViewModels;
using PikaCore.Services;

namespace PikaCore.Controllers.App
{
    [Route("/{controller}/{action}")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileLoggerService _loggerService;
        private readonly IUrlGenerator _urlUrlGeneratorService;

        [TempData] 
        public string StatusMessage { get; set; }
        [TempData(Key = "newPassword")]
        public string NewPassword { get; set; }

        public AdminController(
            UserManager<ApplicationUser> userManager,
            IFileLoggerService loggerService,
            IUrlGenerator urlGenerator)
        {
            _userManager = userManager;
            _loggerService = loggerService;
            _urlUrlGeneratorService = urlGenerator;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            var usersWithRoles = new Dictionary<ApplicationUser, IList<string>>();

            if (usersWithRoles.Count == 0)
            {
                (_userManager.Users.ToListAsync().Result).ForEach(user =>
                {
                    var roles = _userManager.GetRolesAsync(user).Result;
                    usersWithRoles.Add(user, roles);
                });
            }

            var adminPanelViewModel = new AdminPanelViewModel
            {
                LogsListViewModel = null,
                UsersWithRoles = usersWithRoles
            };
            
            return View(adminPanelViewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Route("{id}")]
        public async Task<IActionResult> GeneratePassword(string id)
        {
            var userModel = await _userManager.FindByIdAsync(id);
            if ((await _userManager.GetLoginsAsync(userModel)).Count != 0)
                return RedirectToAction(nameof(Index));

            var token = await _userManager.GeneratePasswordResetTokenAsync(userModel);
            var guid = Guid.NewGuid().ToString();
            _urlUrlGeneratorService.SetDerivationPrf(KeyDerivationPrf.HMACSHA256);
            var hash = _urlUrlGeneratorService.GenerateId(guid);
            await _userManager.ResetPasswordAsync(userModel, token, hash);
            NewPassword = hash;

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Route("{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var userModel = await _userManager.FindByIdAsync(id);
            var editUserModel = new EditUserModel
            {
                Id = userModel.Id,
                Phone = userModel.PhoneNumber,
                Email = userModel.Email,
                UserName = userModel.UserName,
                Roles = await _userManager.GetRolesAsync(userModel)
            };
            return View(editUserModel);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditConfirmation(EditUserModel editModel)
        {
            var userModel = await _userManager.FindByIdAsync(editModel.Id);
            userModel.Email = editModel.Email;
            userModel.UserName = editModel.UserName;
            userModel.PhoneNumber = editModel.Phone;
            if (editModel.Roles != null)
            {
                await _userManager.AddToRolesAsync(userModel, editModel.Roles);
            }
            var result = await _userManager.UpdateAsync(userModel);
            StatusMessage = result.Succeeded ? "Successfully edited user's information." : "Could not edit user's information.";
            return RedirectToAction(nameof(Index), "Admin");
        }

        [HttpGet]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveFromRole(string id, string roleName)
        {
            var user = await _userManager.FindByIdAsync(id);
            if ((await _userManager.GetRolesAsync(user)).Count > 1)
            {
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded) return RedirectToAction(nameof(Edit), new {@Id = id});

                StatusMessage = "User of id " + id + " couldn't be deleted from role: " + roleName;
                _loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), StatusMessage + "\n" + result.Errors);
            }
            else
            {
                StatusMessage = "Couldn't delete user of id " + id + " from role " + roleName + ", user has to be in one role at least.";
            }
            return RedirectToAction(nameof(Edit), new { @Id = id });
        }

        [HttpGet]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin")]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var email = await _userManager.FindByIdAsync(id);
            return View(email);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin")]
        [Route("{id}")]
        public async Task<IActionResult> DeleteConfirmation(string id)
        {
            var result = await _userManager.DeleteAsync(await _userManager.FindByIdAsync(id));
            StatusMessage = result.Succeeded ? "Successfully deleted user of id " + id : "Could not delete user of id " + id;
            return RedirectToAction(nameof(Index), "Admin");
        }

    }
}