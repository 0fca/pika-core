using Microsoft.AspNetCore.Builder;
using PikaCore.Areas.Identity.Middlewares;

namespace PikaCore.Areas.Identity.Extensions;

public static class MinioBucketAccessAuthorizationExtensions
{
    public static IApplicationBuilder UseMinioBucketAccessAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MinioBucketAccessAuthorizationMiddleware>();
    } 
}