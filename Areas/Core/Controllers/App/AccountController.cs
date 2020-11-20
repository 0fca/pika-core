using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Core.Models.AccountViewModels;
using PikaCore.Areas.Core.Security;
using PikaCore.Areas.Infrastructure.Services;

namespace PikaCore.Areas.Core.Controllers.App
{
    [Authorize]
    [Area("Core")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly IMessageService _messageService;

        public AccountController(UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, 
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IMessageService messageService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _messageService = messageService;
        }

        [TempData] private string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = "/")
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            var message = "";
            try
            {
                message = (await _messageService.GetMessageById(3)).Message;
            }
            catch 
            {
                // ignore
            }

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Message"] = message;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);
            var result = await _signInManager.PasswordSignInAsync(model.Username, 
                model.Password, 
                model.RememberMe, 
                lockoutOnFailure: true);
            if (result.Succeeded)
            {
                _logger.LogInformation($"User of email: {model.Username} logged in.");
                return RedirectPermanent(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(LoginWith2Fa), new { returnUrl, model.RememberMe });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning($"User account of username: {model.Username} locked out.");
                TempData["LockoutMessage"] = "Account has been locked out.";
                return RedirectToAction(nameof(Lockout));
            }
            
            ModelState.AddModelError(string.Empty, $"Invalid login attempt. Username: {model.Username}");
            return View(model);
            
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2Fa(bool rememberMe, string returnUrl = "/")
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var model = new LoginWith2FaViewModel { RememberMe = rememberMe };
            ViewData["ReturnUrl"] = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2Fa(LoginWith2FaViewModel model, bool rememberMe, string returnUrl = "/")
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
            ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = "/")
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = "/")
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);
            
            var user = new ApplicationUser { UserName = model.Username, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, RoleString.User);
                _logger.LogInformation("User created a new account.");
                return RedirectToAction("Index", "Manage");
            }
            AddErrors(result);

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        [AutoValidateAntiforgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/")
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]

        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/", string remoteError = null)
        {
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToAction(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation($"User logged in with {info.ProviderDisplayName} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["LoginProvider"] = info.LoginProvider;
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);

            return View("ExternalLogin", new ExternalLoginViewModel { Email = email });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model,
            string returnUrl = "/")
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    throw new ApplicationException("Error loading external login information during confirmation.");
                }

                var user = new ApplicationUser {UserName = model.Username, Email = model.Email};

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }

                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(nameof(ExternalLogin), model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }
                
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
            //TODO: This just wont work, email sender is not working.
            await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
            }
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }
        
        [HttpGet]
        [ActionName("ExportData")]
        [Route("[area]/[controller]/[action]")]
        public IActionResult ExportUserData()
        {
            var userExportData = new ExportDataViewModel()
            {
                DataCollections = new List<string>(){ "User Data" }
            };

            var propertyInfos = typeof(ApplicationUser).GetProperties();
            var userRelatedComplexCollections = propertyInfos
                .ToList()
                .Where(prop => !prop.GetType().IsPrimitive);
            var relatedComplexCollections = userRelatedComplexCollections 
                as PropertyInfo[] ?? userRelatedComplexCollections.ToArray();
            if (relatedComplexCollections.ToList().Count == 0)
            {
                relatedComplexCollections.ToList().ForEach(p => 
                    userExportData.DataCollections.Add(p.Name));
            }
            return View(userExportData);
        }
        
        
        [HttpPost]
        [ActionName("ExportData")]
        [Route("[area]/[controller]/[action]")]
        public async Task<IActionResult> ExportUserDataConfirmation(ExportDataViewModel exportDataViewModel)
        {
            //TODO: Add analyzing request data and gathering requested data here, in a service ofc
            ViewData["returnMessage"] = "Your data are gathered from whole system, it can take a while. " +
                                        "Your data will be accessible for download in CSV format as soon as they are ready";
            
            return View();
        }

        [HttpGet]
        public IActionResult Delete()
        {
            var code = Guid.NewGuid().ToString().Split("-")[0];
            var deleteViewModel = new DeleteAccountViewModel()
            {
                ConfirmationCode = code
            };

            return View(deleteViewModel);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Delete(DeleteAccountViewModel deleteAccountViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(deleteAccountViewModel);
            }

            await _signInManager.SignOutAsync();
            var identityResult = await _userManager.DeleteAsync(await _userManager.GetUserAsync(this.User));

            if (identityResult.Succeeded)
            {
                TempData["returnMessage"] = "Your account has been successfully deleted";
                return RedirectToAction("Index", "Home");
            }
            
            TempData["returnMessage"] = "There was a problem during deleting your account";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View();
        }
        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
        #endregion
    }
}
