using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PikaCore.Areas.Identity.Middlewares;

public class OiddictAuthenticationCookieSupportMiddleware
{
   private readonly RequestDelegate _next;

   public OiddictAuthenticationCookieSupportMiddleware(RequestDelegate next)
   {
      _next = next;
   }

   public async Task InvokeAsync(HttpContext context)
   {
      if (context.Request.Cookies.ContainsKey(".AspNet.Identity"))
      {
         var token = context.Request.Cookies[".AspNet.Identity"];
         context.Request.Headers["Authorization"] =
            $"Bearer {token}";
      } 
      await _next(context);
   }
}