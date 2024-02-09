using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Core.Models;

public class Backup: BaseModel
{
    public int JobId { get; set; }
    
    [Required]
    [ForeignKey(nameof(JobId))]
    public BackupJob Job { get; set; }
    
    public DateTime BackupDate { get; set; }
    public string Path { get; set; }
    public long Size { get; set; }
    
    [NotMapped]
    [ValidateNever]
    public bool Compressed => Path.EndsWith(".zip");
    
    [NotMapped]
    [ValidateNever]
    public string FileName => System.IO.Path.GetFileName(Path);
}