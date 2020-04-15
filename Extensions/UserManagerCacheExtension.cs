using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using PikaCore.Models;

namespace PikaCore.Extensions
{
    public static class UserManagerCacheExtension
    {
        public static void StoreUserCookie(this UserManager<ApplicationUser> userManager, 
            IDistributedCache distributedCache, string userId, string userCookie)
        {
              
        }
    }
}