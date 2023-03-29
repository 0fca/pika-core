using System;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PikaCore.Areas.Identity.Middlewares;

public class EnsureJwtBearerValidMiddleware
{
    private readonly RequestDelegate _next;

    public EnsureJwtBearerValidMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Cookies[".AspNet.ShrCk"];
        if (string.IsNullOrEmpty(token))
        {
            await _next(context);
            return;
        }
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token);
        var jwst = jsonToken as JwtSecurityToken;
        if (jwst!.ValidTo.ToLocalTime() < DateTime.Now.ToLocalTime())
        {
            context.Response.Cookies.Delete(".AspNet.ShrCk");
            await _next(context);
            return;
        }
        await _next(context);
    }
}