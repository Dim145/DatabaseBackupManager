using DatabaseBackupManager.Models;
using Microsoft.AspNetCore.Identity;
using Minio.DataModel.Encryption;

namespace DatabaseBackupManager.Data;

internal enum DatabaseType
{
    Postgres,
    Sqlite
}

internal static class Seeds
{
    internal static MailSettings MailSettings { get; private set; }
    internal static DataSettings DataSettings { get; private set; }
    internal static StorageSettings StorageSettings { get; private set; }
    internal static DatabaseType DatabaseType { get; private set; }
    internal static string DatabaseConnectionString { get; private set; }
    internal static string HangfireConnectionString { get; private set; }

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
            UseSsl = bool.TryParse(Environment.GetEnvironmentVariable("MailSettings__UseSsl"), out var mailUseSsl) && mailUseSsl,
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
        
        StorageSettings = parameters.GetSection("StorageSettings").Get<StorageSettings>() ?? new StorageSettings
        {
            TempPath = Environment.GetEnvironmentVariable("StorageSettings__TempPath") ?? "/tmp/s3",
            S3Bucket = Environment.GetEnvironmentVariable("StorageSettings__S3Bucket"),
            S3DaysRetention = int.TryParse(Environment.GetEnvironmentVariable("StorageSettings__S3DaysRetention"), out var days) ? days : null,
            ServerSideEncryption = Environment.GetEnvironmentVariable("StorageSettings__ServerSideEncryption") switch
            {
                "SSE-S3" => new SSES3(),
                _ => null
            },
            StorageType = Environment.GetEnvironmentVariable("StorageSettings__StorageType") ?? "Local",
            AccessKey = Environment.GetEnvironmentVariable("StorageSettings__AccessKey"),
            SecretKey = Environment.GetEnvironmentVariable("StorageSettings__SecretKey"),
            S3Endpoint = Environment.GetEnvironmentVariable("StorageSettings__S3Endpoint"),
            S3UseSSL = bool.TryParse(Environment.GetEnvironmentVariable("StorageSettings__S3UseSSL"), out var s3UseSSL) && s3UseSSL,
            S3Region = Environment.GetEnvironmentVariable("StorageSettings__S3Region"),
            S3LinkExpiration = int.TryParse(Environment.GetEnvironmentVariable("StorageSettings__S3LinkExpiration"), out var expiration) ? expiration : 60,
        };

        DatabaseType = Enum.TryParse<DatabaseType>(Environment.GetEnvironmentVariable("DatabaseType"), out var dbType) ? dbType : parameters.GetValue<DatabaseType?>("DatabaseType") ?? DatabaseType.Sqlite;

        DatabaseConnectionString = Environment.GetEnvironmentVariable("DefaultConnection") ??
                                   parameters.GetConnectionString("DefaultConnection");
        HangfireConnectionString = Environment.GetEnvironmentVariable("HangfireDb") ??
                                   parameters.GetConnectionString("Hangfire") ??
                                   DatabaseConnectionString;
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