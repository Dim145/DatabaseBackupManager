using Core.Models;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Controllers;

[Route("agents")]
[Authorize(Policy = nameof(Policies.AdminRolePolicy))]
public class AgentController: Controller
{
    private ApplicationDbContext DbContext { get; }
    
    public AgentController(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<IActionResult> Index(string name = null, DatabaseTypes? type = null, bool? active = null)
    {
        var query = DbContext.Agents.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(a => a.Name.Contains(name));

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        if (active.HasValue)
            query = query.Where(a => a.Active == active.Value);
        
        var agents = await query.ToListAsync();
        
        ViewBag.SearchName = name;
        ViewBag.SearchType = type;
        ViewBag.SearchActive = active;
        
        return View(agents);
    }
    
    [HttpGet("new")]
    public IActionResult Create()
    {
        return View(new Agent());
    }
    
    [HttpPost("new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Agent agent)
    {
        if (!ModelState.IsValid)
        {
            return View(agent);
        }

        var arrayOfGuid = new[]
        {
            Guid.NewGuid().ToString("X"),
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid().ToString("D")
        };
        
        // random sort the array
        Array.Sort(arrayOfGuid, (x, y) => new Random().Next(-1, 2));
        
        agent.Active = false;
        agent.Token = string.Join("", arrayOfGuid);
        
        await DbContext.Agents.AddAsync(agent);
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Details), new { id = agent.Id });
    }
    
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var agent = await DbContext.Agents.FindAsync(id);
        
        if (agent == null || DateTime.UtcNow - agent.CreatedAt.ToUniversalTime() > TimeSpan.FromMinutes(5))
            return NotFound();
        
        return View(agent);
    }
    
    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var agent = await DbContext.Agents.FindAsync(id);
        
        if (agent == null)
            return NotFound();
        
        return View(agent);
    }
    
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int id)
    {
        var agentToDelete = await DbContext.Agents.FindAsync(id);
        
        if (agentToDelete == null)
            return NotFound();
        
        DbContext.Agents.Remove(agentToDelete);
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet("{id:int}/changeActive")]
    public async Task<IActionResult> ChangeActiveState(int id)
    {
        var agent = await DbContext.Agents.FindAsync(id);
        
        if (agent == null)
            return NotFound();
        
        agent.Active = !agent.Active;
        
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
}