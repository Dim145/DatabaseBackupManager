using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DatabaseBackupManager.Data.Models;

public class BackupJob: BaseModel
{
    private string[] _databases;
    
    public BackupJob()
    {
        Backups = new();
    }
    
    public string Name { get; set; }
    public string Cron { get; set; }
    public bool Enabled { get; set; }
    
    [ValidateNever]
    public string DatabaseNames
    {
        get => _databases is null ? null : string.Join(",", _databases);
        set => _databases = value?.Split(',');
    }
    
    [NotMapped]
    public string[] Databases
    {
        get => _databases;
        set => _databases = value;
    }
    
    public string BackupFormat { get; set; }
    
    public int ServerId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ServerId))]
    public Server Server { get; set; }
    
    public List<Backup> Backups { get; set; }
}