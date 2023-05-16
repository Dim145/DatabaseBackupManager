using System.Data;
using System.Data.Common;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Data.Models;
using DatabaseBackupManager.Models;
using DatabaseBackupManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Npgsql;

namespace DatabaseBackupManager.Controllers;

[Route("servers")]
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
            return View(server);
        
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
        
        DbContext.Servers.Update(server);
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