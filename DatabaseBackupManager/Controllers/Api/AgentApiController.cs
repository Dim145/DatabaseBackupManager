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
    
    [HttpPost("notify-presence")]
    public IActionResult NotifyPresence([FromBody]  string token)
    {
        var agent = DbContext.Agents.FirstOrDefault(a => a.Token == token);
        
        if (agent == null)
            return NotFound();

        agent.Url = new Uri(Request.Headers["Agent-Url"]);
        agent.LastSeen = DateTime.UtcNow;

        return Ok();
    }
}