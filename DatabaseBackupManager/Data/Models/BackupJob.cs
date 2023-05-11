using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatabaseBackupManager.Data.Models;

public class BackupJob: BaseModel
{
    public BackupJob()
    {
        Backups = new();
    }
    
    public string Name { get; set; }
    public string Cron { get; set; }
    public bool Enabled { get; set; }
    
    [Required]
    [DataType(DataType.Text)]
    public string DatabaseNames { get; set; }
    
    public string BackupFormat { get; set; }
    
    public int ServerId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ServerId))]
    public Server Server { get; set; }
    
    public List<Backup> Backups { get; set; }
}