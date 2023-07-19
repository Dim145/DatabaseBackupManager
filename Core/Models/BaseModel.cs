using System.ComponentModel.DataAnnotations;

namespace Core.Models;

public abstract class BaseModel
{
    protected BaseModel()
    {
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }
    
    [Key]
    public int Id { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}