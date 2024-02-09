using Core.Models;
using DatabaseBackupManager.Models;
using DatabaseBackupManager.Services;
using DatabaseBackupManager.Services.StorageService;
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
    public virtual DbSet<Agent> Agents { get; set; }
    
    private List<Action> BeforeSaveChangesCallbacks { get; }
    private List<Action> AfterSaveChangesCallbacks { get; }

    private IEnumerable<AfterSaveChanges> _changes;
    
    private static string _passwordKey;
    
    private IStorageService StorageService { get; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration conf, IStorageService storageService)
        : base(options)
    {
        StorageService = storageService;
        
        BeforeSaveChangesCallbacks =
        [
            UpdateTimestamps,
            () => _changes = ChangeTracker.Entries().Where(p => p is { Entity: BaseModel })
                .Select(p => new AfterSaveChanges(p)).ToList()
        ];
        
        AfterSaveChangesCallbacks =
        [
            SynchronizeBackupJobsWithHangfire,
            SynchronizeBackupWithFiles
        ];

        if (string.IsNullOrWhiteSpace(_passwordKey))
        {
            _passwordKey = EncryptedStringConverter.GetKey(conf);
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Server>(entity =>
        {
            entity
                .Property(s => s.Password)
                .HasConversion(p => p.Encrypt(_passwordKey),
                    p => p.Decrypt(_passwordKey));
        });
        
        base.OnModelCreating(builder);
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

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        CallBeforeSaveChangesCallbacks();
        
        var res = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        
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

            try
            {
                if (StorageService.Exists(path).GetAwaiter().GetResult())
                    StorageService.Delete(path).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }

    private void SynchronizeBackupJobsWithHangfire()
    {
        var changedBackupJobTrackers = _changes
            .Where(x => x is { Entity: BackupJob })
            .ToList();

        foreach (var backupJobTracker in changedBackupJobTrackers)
        {
            if(backupJobTracker.Entity is not BackupJob backupJob)
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