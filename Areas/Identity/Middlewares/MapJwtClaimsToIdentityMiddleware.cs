using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using Pika.Domain.Security;

namespace PikaCore.Areas.Identity.Middlewares;

public class MapJwtClaimsToIdentityMiddleware
{
    private readonly RequestDelegate _next;

    public MapJwtClaimsToIdentityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Cookies[".AspNet.Identity"];
        if (string.IsNullOrEmpty(token))
        {
            context.User.AddClaim(ClaimTypes.Role, RoleString.User);
            await _next(context);
            return;
        }
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token);
        var jwst = jsonToken as JwtSecurityToken;
        jwst!.Claims.ToList().ForEach(c =>
        {
            if (c.Type == ClaimTypes.Role)
            {
                var roleClaimTypeName = context.User.Identities.First().RoleClaimType;
                context.User.AddClaim(roleClaimTypeName, c.Value); 
            }
            context.User.AddClaim(c.Type, c.Value, c.Issuer);
        });
        await _next(context);
    }
}