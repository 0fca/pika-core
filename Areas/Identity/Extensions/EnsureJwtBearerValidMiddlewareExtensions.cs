using Microsoft.AspNetCore.Builder;
using PikaCore.Areas.Identity.Middlewares;

namespace PikaCore.Areas.Identity.Extensions;

public static class EnsureJwtBearerValidMiddlewareExtensions
{
    public static IApplicationBuilder UseEnsureJwtBearerValid(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EnsureJwtBearerValidMiddleware>();
    }
}