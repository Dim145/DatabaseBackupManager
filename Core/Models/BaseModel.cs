using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public abstract class BaseModel
{
    protected BaseModel()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    [Key]
    public int Id { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}