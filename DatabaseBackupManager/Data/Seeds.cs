using DatabaseBackupManager.Models;
using FluentEmail.Core;
using Microsoft.AspNetCore.Identity;

namespace DatabaseBackupManager.Data;

internal static class Seeds
{
    internal static MailSettings MailSettings { get; private set; }
    internal static DataSettings DataSettings { get; private set; }

    internal static void InitSettingsVars(this IConfiguration parameters)
    {
        MailSettings = parameters.GetSection("MailSettings").Get<MailSettings>() ?? new MailSettings
        {
            From = Environment.GetEnvironmentVariable("MailSettings__From"),
            Host = Environment.GetEnvironmentVariable("MailSettings__Host"),
            Username = Environment.GetEnvironmentVariable("MailSettings__UserName"),
            Password = Environment.GetEnvironmentVariable("MailSettings__Password"),
            FromName = Environment.GetEnvironmentVariable("MailSettings__FromName"),
            Port = int.TryParse(Environment.GetEnvironmentVariable("MailSettings__Port"), out var port) ? port : 587,
            UseSsl = bool.TryParse(Environment.GetEnvironmentVariable("MailSettings__UseSsl"), out var useSsl) && useSsl,
        };
        
        DataSettings = parameters.GetSection("DataSettings").Get<DataSettings>() ?? new DataSettings
        {
            DefaultAdminRole = Environment.GetEnvironmentVariable("DataSettings__DefaultAdminRole") ?? "Admin",
            DefaultAdminEmail = Environment.GetEnvironmentVariable("DataSettings__DefaultAdminEmail") ?? "admin@tochange.com",
            DefaultAdminPassword = Environment.GetEnvironmentVariable("DataSettings__DefaultAdminPassword") ?? "Admin183!!",
            DefaultReaderRole = Environment.GetEnvironmentVariable("DataSettings__DefaultReaderRole") ?? "Reader",
            DefaultEditorRole = Environment.GetEnvironmentVariable("DataSettings__DefaultEditorRole") ?? "Editor",
            DefaultRestorerRole = Environment.GetEnvironmentVariable("DataSettings__DefaultRestorerRole") ?? "Restorer",
        };
    }
    
    internal static async Task SeedDatabase(this IServiceProvider services)
    {
        // Create roles if not exists
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        foreach (var p in typeof(DataSettings).GetProperties().Where(p => p.Name.EndsWith("Role")))
        {
            var roleString = p.GetValue(DataSettings)?.ToString();
            
            if(string.IsNullOrWhiteSpace(roleString))
                continue;
            
            var role = await roleManager.FindByNameAsync(roleString);
            
            if (role == null)
                await roleManager.CreateAsync(new IdentityRole(roleString));
        }

        // Create admin user if not exists
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await userManager.FindByNameAsync(DataSettings.DefaultAdminEmail);

        if (admin == null)
        {
            admin = new IdentityUser(DataSettings.DefaultAdminEmail)
            {
                Email = DataSettings.DefaultAdminEmail,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(admin, DataSettings.DefaultAdminPassword);
        }

        if (!await userManager.IsInRoleAsync(admin, DataSettings.DefaultAdminRole))
            await userManager.AddToRoleAsync(admin, DataSettings.DefaultAdminRole);
    }
}