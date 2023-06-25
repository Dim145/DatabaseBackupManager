using System.Net;
using System.Net.Mail;
using System.Reflection;
using DatabaseBackupManager.Authorizations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Middleware;
using DatabaseBackupManager.Services;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.InitSettingsVars();

// Add services to the container.
var dataConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var hangfireDbPath = builder.Configuration.GetValue<string>("HangfireDbPath") ?? Environment.GetEnvironmentVariable("HangfireDbPath") ?? "hangfire.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(dataConnectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddHangfire(config => 
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseSQLiteStorage(hangfireDbPath)
        .UseRecommendedSerializerSettings()
    );

builder.Services.AddHangfireServer();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = !string.IsNullOrWhiteSpace(Seeds.MailSettings.Host);
        options.SignIn.RequireConfirmedEmail = !string.IsNullOrWhiteSpace(Seeds.MailSettings.Host);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

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
var context = services.GetRequiredService<ApplicationDbContext>();
context.Database.Migrate();

await services.SeedDatabase();
await HangfireService.InitHangfireRecurringJob(context, builder.Configuration);

app.Run();
