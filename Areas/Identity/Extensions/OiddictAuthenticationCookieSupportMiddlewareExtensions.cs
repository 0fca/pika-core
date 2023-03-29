using Microsoft.AspNetCore.Builder;
using PikaCore.Areas.Identity.Middlewares;

namespace PikaCore.Areas.Identity.Extensions;

public static class OiddictAuthenticationCookieSupportMiddlewareExtensions
{
    public static IApplicationBuilder UseOiddictAuthenticationCookieSupport(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OiddictAuthenticationCookieSupportMiddleware>();
    }
}