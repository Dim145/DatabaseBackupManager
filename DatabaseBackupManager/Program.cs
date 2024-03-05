using System.Net;
using System.Net.Mail;
using System.Reflection;
using Azure.Storage;
using Core.Services;
using DatabaseBackupManager.Authorizations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Data.Postgres;
using DatabaseBackupManager.Data.Sqlite;
using DatabaseBackupManager.Middleware;
using DatabaseBackupManager.Services;
using DatabaseBackupManager.Services.StorageService;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Azure;
using Minio;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.InitSettingsVars();

switch (Seeds.StorageSettings.StorageType)
{
    case "S3":
        builder.Services.AddMinio(configureClient =>
        {
            configureClient
                .WithHttpClient(new HttpClient(), false)
                .WithRegion(Seeds.StorageSettings.S3Region)
                .WithEndpoint(Seeds.StorageSettings.S3Endpoint)
                .WithCredentials(Seeds.StorageSettings.AccessKey, Seeds.StorageSettings.SecretKey);

            if (Seeds.StorageSettings.S3UseSSL)
                configureClient.WithSSL();
        }, ServiceLifetime.Scoped);
        
        builder.Services.AddScoped<IStorageService, S3StorageService>();
        break;
    case "AWS3":
        builder.Services.AddScoped<IStorageService, AmazonS3Storage>();
        break;
    case "Azure":
        builder.Services.AddScoped<IStorageService, AzureStorageService>();
        break;
    default:
        builder.Services.AddScoped<IStorageService, LocalStorageService>();
        break;
}

void getOptions(DbContextOptionsBuilder options)
{
    switch (Seeds.DatabaseType)
    {
        case DatabaseType.Postgres:
            options.UseNpgsql(Seeds.DatabaseConnectionString);
            break;
        default:
            options.UseSqlite(Seeds.DatabaseConnectionString);
            break;
    }
}

switch (Seeds.DatabaseType)
{
    case DatabaseType.Postgres:
        builder.Services.AddDbContext<BaseContext, PostgresContext>(getOptions);
        break;
    default:
        builder.Services.AddDbContext<BaseContext, SqliteContext>(getOptions);
        break;
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
    config.UseSimpleAssemblyNameTypeSerializer();

    switch (Seeds.DatabaseType)
    {
        case DatabaseType.Postgres:
            config.UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(Seeds.HangfireConnectionString);
            });
            break;
        default:
            config.UseSQLiteStorage(Seeds.HangfireConnectionString);
            break;
    }
    
    config.UseRecommendedSerializerSettings();
});

builder.Services.AddHangfireServer();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = !string.IsNullOrWhiteSpace(Seeds.MailSettings.Host);
        options.SignIn.RequireConfirmedEmail = !string.IsNullOrWhiteSpace(Seeds.MailSettings.Host);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<BaseContext>();

builder.Services.AddAuthorization(options =>
{
    foreach (var methodInfo in typeof(Policies).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name.EndsWith("Policy")))
    {
        options.AddPolicy(methodInfo.Name, policy => methodInfo.Invoke(null, new object[] {policy}));
    }
});

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<PostgresBackupService>();
builder.Services.AddScoped<HangfireService>();
builder.Services.AddScoped<LogoutMiddleware>();

builder.Services.AddHostedService<FileWatcherService>();

if (Seeds.MailSettings.IsValid())
{
    builder.Services.AddFluentEmail(Seeds.MailSettings.From ?? Seeds.MailSettings.Username, Seeds.MailSettings.FromName)
        .AddRazorRenderer()
        .AddSmtpSender(new SmtpClient
        {
            Host = Seeds.MailSettings.Host,
            Port = Seeds.MailSettings.Port,
            Credentials = new NetworkCredential(Seeds.MailSettings.Username, Seeds.MailSettings.Password),
            EnableSsl = Seeds.MailSettings.UseSsl
        });

    builder.Services.AddTransient<IEmailSender, EmailSender>();
}
else
{
    Console.Error.WriteLine("Mail settings are not valid. Email confirmation will not work.\nconfig: " + Seeds.MailSettings);
}

var googleSection = builder.Configuration.GetSection("Authentication:Google");
var googleClientId = googleSection["ClientId"] ?? Environment.GetEnvironmentVariable("GoogleClientId");
var googleClientSecret = googleSection["ClientSecret"] ?? Environment.GetEnvironmentVariable("GoogleClientSecret");

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new []{ new HangfireAuth() },
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.UseMiddleware<LogoutMiddleware>();

// Run migrations
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var context = services.GetRequiredService<BaseContext>();
context.Database.Migrate();

await services.SeedDatabase();
await HangfireService.InitHangfireRecurringJob(context, builder.Configuration);

try
{
    app.Run();
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
}
finally
{
    if(Directory.Exists(Core.Constants.TempDirForBackups))
        Directory.Delete(Core.Constants.TempDirForBackups, true);
}
