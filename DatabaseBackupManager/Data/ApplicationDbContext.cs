using DatabaseBackupManager.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public virtual DbSet<Server> Servers { get; set; }
    public virtual DbSet<BackupJob> BackupJobs { get; set; }
    public virtual DbSet<Backup> Backups { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        
        return base.SaveChanges();
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        UpdateTimestamps();
        
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateTimestamps();
        
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        UpdateTimestamps();
        
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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
}