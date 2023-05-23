using System.Net;
using System.Net.Mail;
using DatabaseBackupManager.Authorizations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Models;
using DatabaseBackupManager.Services;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

var defaultAdminRole = builder.Configuration["DefaultAdminRole"] ?? Environment.GetEnvironmentVariable("DefaultAdminRole") ?? "Admin";
var defaultAdminEmail = builder.Configuration["DefaultAdminEmail"] ?? Environment.GetEnvironmentVariable("DefaultAdminEmail") ?? "admin@tochange.com";
var defaultAdminPassword = builder.Configuration["DefaultAdminPassword"] ?? Environment.GetEnvironmentVariable("DefaultAdminPassword") ?? "Admin183!!";
var mailSetting = builder.Configuration.GetSection("MailSettings").Get<MailSettings>();

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
        options.SignIn.RequireConfirmedAccount = !string.IsNullOrWhiteSpace(mailSetting?.Host);
        options.SignIn.RequireConfirmedEmail = !string.IsNullOrWhiteSpace(mailSetting?.Host);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminRolePolicy", policy =>
    {
        policy.RequireRole(defaultAdminRole);
    });
});

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<PostgresBackupService>();
builder.Services.AddScoped<HangfireService>();

if (!string.IsNullOrWhiteSpace(mailSetting?.Host))
{
    builder.Services.AddFluentEmail(mailSetting.From ?? mailSetting.User, mailSetting.FromName)
        .AddRazorRenderer()
        .AddSmtpSender(new SmtpClient
        {
            Host = mailSetting.Host,
            Port = mailSetting.Port,
            Credentials = new NetworkCredential(mailSetting.User, mailSetting.Password)
        });

    builder.Services.AddTransient<IEmailSender, EmailSender>();
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
    app.UseHttpsRedirection();
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

// Run migrations
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var context = services.GetRequiredService<ApplicationDbContext>();
context.Database.Migrate();

// Create roles if not exists
var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
var adminRole = await roleManager.FindByNameAsync(defaultAdminRole);

if (adminRole == null)
    await roleManager.CreateAsync(new IdentityRole(defaultAdminRole));

// Create admin user if not exists
var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
var admin = await userManager.FindByNameAsync(defaultAdminEmail);

if (admin == null)
{
    admin = new IdentityUser(defaultAdminEmail)
    {
        Email = defaultAdminEmail,
        EmailConfirmed = true
    };

    await userManager.CreateAsync(admin, defaultAdminPassword);
}

if (!await userManager.IsInRoleAsync(admin, defaultAdminRole))
    await userManager.AddToRoleAsync(admin, defaultAdminRole);

await HangfireService.InitHangfireRecurringJob(context, builder.Configuration);

app.Run();