using Hangfire.Dashboard;
using Microsoft.AspNetCore.Identity;

namespace DatabaseBackupManager.Authorizations;

public class HangfireAuth: IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var signInManager = httpContext.RequestServices.GetService<SignInManager<IdentityUser>>();

        if (!(signInManager?.IsSignedIn(signInManager.Context.User) ?? false))
            return false;

        var userManager = signInManager.UserManager;

        var user = userManager!.GetUserAsync(httpContext.User).GetAwaiter().GetResult();
        var roles = userManager!.GetRolesAsync(user).GetAwaiter().GetResult();

        return roles.Contains("Admin");
    }
}