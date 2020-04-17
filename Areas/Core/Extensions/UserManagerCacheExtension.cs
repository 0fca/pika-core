using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using PikaCore.Areas.Core.Models;
using PikaCore.Models;

namespace PikaCore.Areas.Core.Extensions
{
    public static class UserManagerCacheExtension
    {
        public static void StoreUserCookie(this UserManager<ApplicationUser> userManager, 
            IDistributedCache distributedCache, string userId, string userCookie)
        {
              
        }
    }
}