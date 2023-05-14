using DatabaseBackupManager.Data.Models;
using DatabaseBackupManager.Services;
using Hangfire;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static DatabaseBackupManager.Constants;

namespace DatabaseBackupManager.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public virtual DbSet<Server> Servers { get; set; }
    public virtual DbSet<BackupJob> BackupJobs { get; set; }
    public virtual DbSet<Backup> Backups { get; set; }
    
    private List<Action> SaveChangesCallbacks { get; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        SaveChangesCallbacks = new List<Action>
        {
            UpdateTimestamps,
            SynchronizeBackupJobsWithHangfire
        };
    }

    public override int SaveChanges()
    {
        CallSaveChangesCallbacks();
        
        return base.SaveChanges();
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        CallSaveChangesCallbacks();
        
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        CallSaveChangesCallbacks();
        
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        CallSaveChangesCallbacks();
        
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void CallSaveChangesCallbacks()
    {
        SaveChangesCallbacks.ForEach(callback => callback());
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

    private void SynchronizeBackupJobsWithHangfire()
    {
        var changedBackupJobTrackers = ChangeTracker.Entries()
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