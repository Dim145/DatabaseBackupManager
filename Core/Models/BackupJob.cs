using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Core.Models;

public class BackupJob: BaseModel
{
    public BackupJob()
    {
        Backups = new List<Backup>();
    }
    
    public string Name { get; set; }
    public string Cron { get; set; }
    public bool Enabled { get; set; }
    
    [DisplayFormat(DataFormatString = "{0:dd\\:hh\\:mm\\:ss}", ApplyFormatInEditMode = true)]
    public TimeSpan Retention { get; set; }
    
    [ValidateNever]
    public string DatabaseNames
    {
        get => Databases is null ? null : string.Join(",", Databases);
        set => Databases = value?.Split(',');
    }
    
    [NotMapped]
    public string[] Databases { get; set; }

    public string BackupFormat { get; set; }
    
    public int ServerId { get; set; }
    
    [NotMapped]
    public IDatabase Server { get; set; }
    
    
    public string ServerType { get; set; }
    
    public List<Backup> Backups { get; set; }
}