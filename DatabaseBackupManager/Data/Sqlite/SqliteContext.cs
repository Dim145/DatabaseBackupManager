using Core.Models;
using DatabaseBackupManager.Services.StorageService;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Data.Sqlite;

public class SqliteContext(
    DbContextOptions<SqliteContext> options,
    IConfiguration conf,
    IStorageService storageService)
    : BaseContext(options, conf, storageService)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BackupJob>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("datetime()")
                .HasConversion(
                    v => v.Ticks,
                    v => new DateTime(v))
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("datetime()")
                .HasConversion(
                    v => v.Ticks,
                    v => new DateTime(v))
                .ValueGeneratedOnAddOrUpdate();
        });
        
        modelBuilder.Entity<Server>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("datetime()")
                .HasConversion(
                    v => v.Ticks,
                    v => new DateTime(v))
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("datetime()")
                .HasConversion(
                    v => v.Ticks,
                    v => new DateTime(v))
                .ValueGeneratedOnAddOrUpdate();
        });
        
        modelBuilder.Entity<Backup>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("datetime()")
                .HasConversion(
                    v => v.Ticks,
                    v => new DateTime(v))
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("datetime()")
                .HasConversion(
                    v => v.Ticks,
                    v => new DateTime(v))
                .ValueGeneratedOnAddOrUpdate();
            
            entity.Property(e => e.BackupDate)
                .HasConversion(
                    v => v.Ticks,
                    v => new DateTime(v))
                .HasDefaultValueSql("datetime()")
                .ValueGeneratedOnAdd();
        });
    }
}