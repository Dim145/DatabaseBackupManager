using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models;

public class Agent: BaseModel
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

    [NotMapped]
    public AgentState State => LastSeen switch
    {
        null => AgentState.Waiting,
        _ when DateTime.UtcNow - LastSeen?.ToUniversalTime() > TimeSpan.FromMinutes(5) => AgentState.NotResponding,
        _ => AgentState.Running
    };
}