using DatabaseBackupManager.Data;
using DatabaseBackupManager.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Controllers;

[Authorize(Policy = nameof(Policies.AdminRolePolicy))]
[Route("rolebinding")]
public class RoleBindingController: Controller
{
    private RoleManager<IdentityRole> RoleManager { get; }
    private UserManager<IdentityUser> UserManager { get; }
    private BaseContext DbContext { get; }
    private SignInManager<IdentityUser> SignInManager { get; }
    

    public RoleBindingController(
        RoleManager<IdentityRole> roleManager, 
        UserManager<IdentityUser> userManager, 
        BaseContext dbContext,
        SignInManager<IdentityUser> signInManager
        )
    {
        RoleManager = roleManager;
        UserManager = userManager;
        DbContext = dbContext;
        SignInManager = signInManager;
    }
    
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var roles = await RoleManager.Roles.ToListAsync();
        var users = await UserManager.Users.ToListAsync();
        var identityRoles = await DbContext.UserRoles.ToListAsync();

        ViewBag.Roles = roles;
        ViewBag.Users = users;
        ViewBag.IdentityRoles = identityRoles;
        
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> Index(string userId, string roleId)
    {
        var user = await UserManager.FindByIdAsync(userId);
        var role = await RoleManager.FindByIdAsync(roleId);

        if (user == null || role == null)
            return RedirectToAction("Index");
        
        await UserManager.AddToRoleAsync(user, role.Name!);
        
        LogoutMiddleware.RequestUserToDisconnect(user.UserName);

        return RedirectToAction("Index");
    }
    
    [HttpGet("delete")]
    public async Task<IActionResult> Delete(string userId, string roleId)
    {
        var user = await UserManager.FindByIdAsync(userId);
        var role = await RoleManager.FindByIdAsync(roleId);

        if (user == null || role == null || User.Identity!.Name == user.UserName)
            return RedirectToAction("Index");
        
        await UserManager.RemoveFromRoleAsync(user, role.Name!);
        
        LogoutMiddleware.RequestUserToDisconnect(user.UserName);
        
        return RedirectToAction("Index");
    }
}