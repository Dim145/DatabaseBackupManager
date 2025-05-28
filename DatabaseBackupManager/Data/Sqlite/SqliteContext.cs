using System.Globalization;
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
            // add auto generate id on add with max id + 1
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("COALESCE((SELECT MAX(id) FROM BackupJobs), 0) + 1");
            
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
                .ValueGeneratedOnAdd();
            
            entity.Property(e => e.Retention)
                .HasDefaultValueSql("7 days")
                .HasConversion(
                    v => v.ToString(@"dd\.hh\:mm\:ss"),
                    v => TimeSpan.ParseExact(v, @"dd\.hh\:mm\:ss", CultureInfo.InvariantCulture))
                .ValueGeneratedOnAdd();
        });
        
        modelBuilder.Entity<Server>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("COALESCE((SELECT MAX(id) FROM Servers), 0) + 1");
            
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
                .ValueGeneratedOnAdd();
        });
        
        modelBuilder.Entity<Backup>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("COALESCE((SELECT MAX(id) FROM Backups), 0) + 1");
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("datetime()")
                .HasConversion(
                    v => v.Ticks,
                    v => new DateTime(v))
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValue(DateTime.MinValue)
                .HasConversion(
                    v => v.Ticks,
                    v => new DateTime(v))
                .ValueGeneratedOnAdd();
            
            entity.Property(e => e.BackupDate)
                .HasConversion(
                    v => v.Ticks,
                    v => new DateTime(v))
                .HasDefaultValueSql("datetime()")
                .ValueGeneratedOnAdd();
        });
        
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("COALESCE((SELECT MAX(id) FROM Agents), 0) + 1");
            
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
                .ValueGeneratedOnAdd();
        });
    }
}