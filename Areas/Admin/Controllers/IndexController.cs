using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pika.Domain.Identity.Data;
using PikaCore.Areas.Admin.Models.Index;
using PikaCore.Areas.Admin.Models.Index.DTO;
using PikaCore.Areas.Core.Services;
using PikaCore.Areas.Identity.Models.ManageViewModels;
using PikaCore.Infrastructure.Services;
using Serilog;

namespace PikaCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("/{area}/{controller}/{action}")]
    [Authorize(Roles = "Admin")]
    [ResponseCache(CacheProfileName = "Default")]
    public class IndexController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUrlGenerator _urlUrlGeneratorService;
        private readonly IMessageService _messageService;

        [TempData] 
        public string StatusMessage { get; set; } = "";
        
        [TempData(Key = "newPassword")]
        public string NewPassword { get; set; }

        public IndexController(
            UserManager<ApplicationUser> userManager,
            IUrlGenerator urlGenerator,
            IMessageService messageService)
        {
            _userManager = userManager;
            _urlUrlGeneratorService = urlGenerator;
            _messageService = messageService;

        }
                
        [HttpGet]
        [Route("/{area}", Name = "Index")]
        public async Task<IActionResult> Index(int offset = 0)
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
            
            var messageViewModel = new MessageViewModel();
            var messages = (await _messageService.GetAllMessages()).ToList();
            const int messagesPerPageCount = 5;
            messageViewModel.OrganizeMessages(ref messages, messagesPerPageCount);
            _messageService.ApplyPaging(ref messages, messagesPerPageCount, offset);
            messageViewModel.Messages = messages;
            
            var adminPanelViewModel = new AdminPanelViewModel
            {
                MessageViewModel = messageViewModel,
                UsersWithRoles = usersWithRoles
            };
            
            return View(adminPanelViewModel);
        }

        [HttpGet]
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
            return RedirectToAction(nameof(Index), "Index");
        }

        [HttpGet]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> RemoveFromRole(string id, string roleName)
        {
            var user = await _userManager.FindByIdAsync(id);
            if ((await _userManager.GetRolesAsync(user)).Count > 1)
            {
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded) return RedirectToAction(nameof(Edit), new {@Id = id});

                StatusMessage = "User of id " + id + " couldn't be deleted from role: " + roleName;
                Log.Error(StatusMessage);
            }
            else
            {
                StatusMessage = "Couldn't delete user of id " + id + " from role " + roleName + ", user has to be in one role at least.";
            }
            return RedirectToAction(nameof(Edit), new { @Id = id });
        }

        [HttpGet]
        [AutoValidateAntiforgeryToken]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var email = await _userManager.FindByIdAsync(id);
            return View(email);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Route("{id}")]
        public async Task<IActionResult> DeleteConfirmation(string id)
        {
            var result = await _userManager.DeleteAsync(await _userManager.FindByIdAsync(id));
            StatusMessage = result.Succeeded ? "Successfully deleted user of id " + id : "Could not delete user of id " + id;
            return RedirectToAction(nameof(Index), "Index");
        }

        [HttpGet]
        public async Task<IActionResult> RemoveMessages()
        {
            var deletedDto = new DeleteMessageDto();
            var messages = await _messageService.GetAllMessages();
            deletedDto.Messages = DeleteMessageDto.MessageListToDto(messages);
            return View(deletedDto);
        }

        [HttpPost]
        [Route("{idsList?}")]
        public async Task<IActionResult> RemoveMessagesExecute(string idsList)
        {
            if (string.IsNullOrEmpty(idsList))
            {
                return BadRequest("No ids given.");
            }
            try
            {
                var intIdsList = idsList.Remove(idsList.Length - 1).Split(",").Select(int.Parse).ToList();
                await _messageService.RemoveMessages(intIdsList);
                return Accepted("/Admin/Index");
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }

            return BadRequest("Couldn't delete messages, because none of them found.");
        }
    }
}