using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
}