using DatabaseBackupManager.Data.Models;
using DatabaseBackupManager.Models;
using DatabaseBackupManager.Services;
using Hangfire;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using static DatabaseBackupManager.Constants;

namespace DatabaseBackupManager.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public virtual DbSet<Server> Servers { get; set; }
    public virtual DbSet<BackupJob> BackupJobs { get; set; }
    public virtual DbSet<Backup> Backups { get; set; }
    
    private List<Action> BeforeSaveChangesCallbacks { get; }
    private List<Action> AfterSaveChangesCallbacks { get; }

    private IEnumerable<AfterSaveChanges> _changes;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        BeforeSaveChangesCallbacks = new List<Action>
        {
            UpdateTimestamps,
            () => _changes = ChangeTracker.Entries().Where(p => p is {Entity: BaseModel}).Select(p => new AfterSaveChanges(p)).ToList()
        };
        
        AfterSaveChangesCallbacks = new List<Action>
        {
            SynchronizeBackupJobsWithHangfire,
            SynchronizeBackupWithFiles
        };
    }

    public override int SaveChanges()
    {
        CallBeforeSaveChangesCallbacks();
        
        var res = base.SaveChanges();
        
        CallAfterSaveChangesCallbacks();
        
        return res;
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        CallBeforeSaveChangesCallbacks();
        
        var res = await base.SaveChangesAsync(cancellationToken);
        
        CallAfterSaveChangesCallbacks();
        
        return res;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        CallBeforeSaveChangesCallbacks();
        
        var res = base.SaveChanges(acceptAllChangesOnSuccess);
        
        CallAfterSaveChangesCallbacks();
        
        return res;
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        CallBeforeSaveChangesCallbacks();
        
        var res = base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        
        CallAfterSaveChangesCallbacks();
        
        return res;
    }

    private void CallBeforeSaveChangesCallbacks()
    {
        BeforeSaveChangesCallbacks.ForEach(callback => callback());
    }
    
    private void CallAfterSaveChangesCallbacks()
    {
        AfterSaveChangesCallbacks.ForEach(callback => callback());
    }

    private void UpdateTimestamps()
    {
        var entities = ChangeTracker.Entries()
            .Where(x => x is { Entity: BaseModel, State: EntityState.Modified })
            .Select(x => x.Entity)
            .Cast<BaseModel>();
        
        foreach (var entity in entities)
            entity.UpdatedAt = DateTime.Now;
    }

    private void SynchronizeBackupWithFiles()
    {
        var changedBackupTrackers = _changes
            .Where(x => x is { Entity: Backup, State: EntityState.Deleted })
            .ToList();

        foreach (var changedBackupTracker in changedBackupTrackers)
        {
            var path = (changedBackupTracker.Entity as Backup)?.Path;
            
            if(string.IsNullOrWhiteSpace(path))
                continue;
            
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private void SynchronizeBackupJobsWithHangfire()
    {
        var changedBackupJobTrackers = _changes
            .Where(x => x is { Entity: BackupJob })
            .ToList();

        foreach (var backupJobTracker in changedBackupJobTrackers)
        {
            var backupJob = backupJobTracker.Entity as BackupJob;
            
            if(backupJob is null)
                continue;
            
            switch (backupJobTracker.State)
            {
                case EntityState.Added:
                    AddOrUpdateHangfireJob(backupJob);
                    break;
                case EntityState.Deleted:
                    RemoveBackupJobFromHangfire(backupJob);
                    break;
                case EntityState.Modified:
                    if (backupJobTracker.Property("Name").IsModified)
                    {
                        var oldName = backupJobTracker.Property("Name").OriginalValue?.ToString();
                    
                        if(string.IsNullOrWhiteSpace(oldName))
                            continue;
                    
                        RecurringJob.RemoveIfExists(GetJobNameForBackupJob(oldName, backupJob.Id));
                        AddOrUpdateHangfireJob(backupJob);
                    }
                    
                    if (backupJobTracker.Property("Cron").IsModified)
                    {
                        AddOrUpdateHangfireJob(backupJob);
                    }

                    if (backupJobTracker.Property("Enabled").IsModified)
                    {
                        if (backupJob.Enabled)
                            AddOrUpdateHangfireJob(backupJob);                    
                        else
                            RemoveBackupJobFromHangfire(backupJob);
                    }
                    break;
            }
        }
    }
}