using DatabaseBackupManager.Data;
using DatabaseBackupManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseBackupManager.Controllers.Api;

[Route("api/agents")]
[ApiController]
public class AgentApiController: ControllerBase
{
    private BaseContext DbContext { get; }
    
    public AgentApiController(BaseContext dbContext)
    {
        DbContext = dbContext;
    }
    
    public class AgentRequest
    {
        public string Token { get; set; }
        public string[] Databases { get; set; }
    }
    
    [HttpPost("notify-presence")]
    public IActionResult NotifyPresence([FromBody] AgentRequest agentRequest)
    {
        var agent = DbContext.Agents.FirstOrDefault(a => a.Token == agentRequest.Token);
        
        if (agent == null)
            return NotFound();

        agent.Url = Uri.TryCreate(Request.Headers["Agent-Url"], UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
        agent.LastSeen = DateTime.UtcNow;
        agent.Databases = agentRequest.Databases;
        
        DbContext.SaveChanges();

        return Ok(agent.JobQueue.Select(j =>
        {
            var isBackup = j.StartsWith("BackupJob");

            var jobId = isBackup ? int.TryParse(j.Split("-").Last(), out var id) ? id : 0 : 0;

            return new
            {
                Name = j,
                Type = isBackup,
                DbContext.BackupJobs.First(job => job.Id == jobId).DatabaseNames
            };
        }));
    }
}