using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Pika.Domain.Identity.Data;
using PikaCore.Areas.Core.Models;

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