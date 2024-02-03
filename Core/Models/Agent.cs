using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models;

public class Agent: BaseModel, IDatabase
{
    [Required]
    public string Name { get; set; }
    
    [DataType(DataType.Url)]
    public Uri Url { get; set; }
    
    [DataType(DataType.Text)]
    public string Token { get; set; }
    
    [Required]
    public DatabaseTypes Type { get; set; }
    
    [DefaultValue(false)]
    public bool Active { get; set; }
    
    public DateTime? LastSeen { get; set; }
    public DateTime? LastUsed { get; set; }
    
    [Column(TypeName = "json")]
    public string[] Databases { get; set; }
    
    [Column(TypeName = "json")]
    public string[] JobQueue { get; set; }

    [NotMapped]
    public AgentState State => LastSeen switch
    {
        null => AgentState.Waiting,
        _ when DateTime.UtcNow - LastSeen?.ToUniversalTime() > TimeSpan.FromMinutes(Constants.TimeBeforeTimeoutAgent) => AgentState.NotResponding,
        _ => AgentState.Running
    };

    public Task<string[]> ListDatabases()
    {
        return Task.FromResult(Databases);
    }
}