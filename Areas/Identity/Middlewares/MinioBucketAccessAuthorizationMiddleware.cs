using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PikaCore.Areas.Identity.Attributes;
using PikaCore.Infrastructure.Adapters;
using PikaCore.Infrastructure.Services;

namespace PikaCore.Areas.Identity.Middlewares;

public class MinioBucketAccessAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public MinioBucketAccessAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        if (!endpoint.Metadata.Any(m => m is AuthorizeUserBucketAccess))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Query.ContainsKey("bucketId"))
        {
            await _next(context);
            return;
        }

        var bucketId = context.Request.Query["bucketId"][0]!;
        var storageService = context.RequestServices.GetRequiredService<IStorage>();
        if (!await storageService.UserHasBucketAccess(Guid.Parse(bucketId),context.User))
        {
             context.Response.Redirect("/Identity/Gateway/Login");  
        }

        await _next(context);
    }
}