using DatabaseBackupManager.Data;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseBackupManager.Controllers.Api;

[Route("api/agents")]
[ApiController]
public class AgentApiController: ControllerBase
{
    private ApplicationDbContext DbContext { get; }
    
    public AgentApiController(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }
    
    [HttpGet("notify-presence")]
    public IActionResult NotifyPresence(string token)
    {
        var agent = DbContext.Agents.FirstOrDefault(a => a.Token == token);
        
        if (agent == null)
            return NotFound();
        
        agent.Url = new Uri(Request.Host.ToUriComponent());
        agent.LastSeen = DateTime.UtcNow;

        return Ok();
    }
}