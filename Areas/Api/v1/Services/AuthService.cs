using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PikaCore.Areas.Core.Models;

namespace PikaCore.Areas.Api.v1.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthService(IConfiguration configuration,
            SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task<string?> Authenticate(string username, string password)
        {
            var userSignInResult = await _signInManager.PasswordSignInAsync(username, password, false, true);
            if (!userSignInResult.Succeeded)
            {
                return null;
            }

            var user = await _signInManager.UserManager.Users
                .FirstAsync(u => u.UserName.Equals(username));

            return user.UserName;
        }

        public async Task<string?> SignOut()
        {
            var userName = (await _signInManager.UserManager.GetUserAsync(_signInManager.Context.User)).UserName;
            try
            {
                await _signInManager.SignOutAsync();
            }
            catch (Exception ex)
            {
                userName = null;
            }
            return userName;
        }
    }
}