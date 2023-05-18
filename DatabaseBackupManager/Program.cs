using DatabaseBackupManager.Authorizations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Services;
using Hangfire;
using Hangfire.Storage.SQLite;

var builder = WebApplication.CreateBuilder(args);

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
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<PostgresBackupService>();

builder.Services.AddScoped<HangfireService>();

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

var defaultAdminRole = builder.Configuration["DefaultAdminRole"] ?? Environment.GetEnvironmentVariable("DefaultAdminRole") ?? "Admin";
var defaultAdminEmail = builder.Configuration["DefaultAdminEmail"] ?? Environment.GetEnvironmentVariable("DefaultAdminEmail") ?? "admin@tochange.com";
var defaultAdminPassword = builder.Configuration["DefaultAdminPassword"] ?? Environment.GetEnvironmentVariable("DefaultAdminPassword") ?? "Admin183!!";

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