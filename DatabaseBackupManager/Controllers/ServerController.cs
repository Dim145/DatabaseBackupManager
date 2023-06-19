using System.Data;
using System.Data.Common;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Data.Models;
using DatabaseBackupManager.Middleware;
using DatabaseBackupManager.Models;
using DatabaseBackupManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Npgsql;

namespace DatabaseBackupManager.Controllers;

[Route("servers")]
[Authorize(Policy = nameof(Policies.AdminRolePolicy))]
public class ServerController: Controller
{
    private ApplicationDbContext DbContext { get; }
    
    public ServerController(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }
    
    public async Task<IActionResult> Index(string sn)
    {
        var query = DbContext.Servers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(sn))
        {
            query = query.Where(s => s.Name.Contains(sn));
        }
        
        var servers = await query.ToListAsync();
        
        ViewBag.SearchName = sn;
        
        return View(servers);
    }
    
    [HttpGet("new")]
    public IActionResult Create()
    {
        return View(new Server());
    }
    
    [HttpPost("new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Server server)
    {
        if (!ModelState.IsValid)
        {
            if(server.Type != DatabaseTypes.Sqlite)
                return View(server);
            
            if (string.IsNullOrWhiteSpace(server.Host))
                return View(server);
        }

        if (server.Type == DatabaseTypes.Sqlite)
        {
            server.User = Constants.DefaultValueForSqliteColumns;
            server.Port = 1;

            if (!System.IO.File.Exists(server.Host))
            {
                ModelState.AddModelError(nameof(Server.Host), "The file does not exist");
                
                return View(server);
            }
        }

        var testConnection = await TestConnection(server);
        
        if (!string.IsNullOrWhiteSpace(testConnection))
        {
            ModelState.AddModelError("ConnectionString", testConnection);
            
            return View(server);
        }
        
        await DbContext.Servers.AddAsync(server);
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var server = await DbContext.Servers.FirstOrDefaultAsync(s => s.Id == id);
        
        if (server is null)
            return NotFound();

        return View(server);
    }
    
    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Server server)
    {
        if (!ModelState.IsValid)
            return View(server);

        if (id != server.Id)
            return BadRequest("Id is not the same as server.Id");
        
        var testConnection = await TestConnection(server);
        
        if (!string.IsNullOrWhiteSpace(testConnection))
        {
            ModelState.AddModelError("ConnectionString", testConnection);
            
            return View(server);
        }
        
        var existingServer = await DbContext.Servers.FirstOrDefaultAsync(s => s.Id == id);
        
        if (existingServer is null)
            return NotFound();
        
        existingServer.Name = server.Name;
        existingServer.Host = server.Host;
        existingServer.Port = server.Port;
        existingServer.Type = server.Type;
        existingServer.User = server.User;
        
        if (!string.IsNullOrWhiteSpace(server.Password))
            existingServer.Password = server.Password;

        DbContext.Servers.Update(existingServer);
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var server = await DbContext.Servers.FirstOrDefaultAsync(s => s.Id == id);
        
        if (server is null)
            return NotFound();
        
        return View(server);
    }
    
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, Server server)
    {
        if (id != server.Id)
            return BadRequest("Id is not the same as server.Id");
        
        DbContext.Servers.Remove(server);
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    
    private async Task<string> TestConnection(Server server)
    {
        if (server is null)
            return string.Empty;

        try
        {
            var serverConnection = server.GetConnection();
            
            await serverConnection.OpenAsync();
            
            var res = serverConnection.State == ConnectionState.Open ? string.Empty : "Connection is not successful";

            await serverConnection.CloseAsync();

            return res;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
}