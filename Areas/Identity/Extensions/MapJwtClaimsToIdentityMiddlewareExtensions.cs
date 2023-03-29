using Microsoft.AspNetCore.Builder;
using PikaCore.Areas.Identity.Middlewares;

namespace PikaCore.Areas.Identity.Extensions;

public static class MapJwtClaimsToIdentityMiddlewareExtensions
{
    public static IApplicationBuilder UseMapJwtClaimsToIdentity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MapJwtClaimsToIdentityMiddleware>();
    }
}