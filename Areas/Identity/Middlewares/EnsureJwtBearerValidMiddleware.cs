using System;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace PikaCore.Areas.Identity.Middlewares;

public class EnsureJwtBearerValidMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public EnsureJwtBearerValidMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Cookies[".AspNet.Identity"];
        if (string.IsNullOrEmpty(token))
        {
            await _next(context);
            return;
        }
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token);
        var jwst = jsonToken as JwtSecurityToken;
        var validTo = jwst!.ValidTo.ToLocalTime();
        var localNow = DateTime.Now.ToLocalTime();
        
        if (validTo <= localNow)
        {
            context.Response.Cookies.Delete(".AspNet.Identity", new CookieOptions
            {
                Path = "/",
                Domain = _configuration.GetSection("Auth")["CookieDomain"]
            });
        }
        await _next(context);
    }
}