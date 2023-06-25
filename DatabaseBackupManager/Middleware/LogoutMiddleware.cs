using Microsoft.AspNetCore.Authentication;

namespace DatabaseBackupManager.Middleware;

public class LogoutMiddleware: IMiddleware
{
    private static List<string> RequestUserDisconnects { get; } = new();
    
    internal static bool RequestUserToDisconnect(string userName)
    {
        if (RequestUserDisconnects.Contains(userName))
            return false;
        
        RequestUserDisconnects.Add(userName);
        return true;
    }
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value;
        
        if (context.User.Identity?.IsAuthenticated is true && RequestUserDisconnects.Contains(context.User.Identity.Name))
        {
            RequestUserDisconnects.Remove(context.User.Identity.Name);
            
            if (path != "/Identity/Account/Logout")
            {
                var authSchemes = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                
                // get all signout schemes and loop them to signout
                foreach (var scheme in await authSchemes.GetRequestHandlerSchemesAsync())
                {
                    try
                    {
                        await context.SignOutAsync(scheme.Name);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                
                context.Response.Redirect("/");
                context.Response.Cookies.Delete(".AspNetCore.Identity.Application");
            }
        }
        else if(path is "/Identity/Account/Login" && context.Request.Method == "POST")
            RequestUserDisconnects.Remove(context.Request.Form["Input.Email"]);

        await next(context);
    }
}