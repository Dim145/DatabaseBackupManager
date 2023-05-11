using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatabaseBackupManager.Data.Models;

public class Backup: BaseModel
{
    public int JobId { get; set; }
    
    [Required]
    [ForeignKey(nameof(JobId))]
    public BackupJob Job { get; set; }
    
    public DateTime BackupDate { get; set; }
    public string Path { get; set; }
}