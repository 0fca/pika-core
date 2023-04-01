using System.Linq;
using System.Security.Claims;
using Hangfire.Dashboard;

namespace PikaCore.Areas.Identity.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var isAdministrator = httpContext.User.Claims
            .FirstOrDefault(c => c is { Type: ClaimTypes.Role, Value: "Administrator" }) != null;
        return (httpContext.User.Identity?.IsAuthenticated ?? false) && isAdministrator;
    }
}