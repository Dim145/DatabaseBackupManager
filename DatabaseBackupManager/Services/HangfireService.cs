using DatabaseBackupManager.Data;
using DatabaseBackupManager.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DatabaseBackupManager.Services;

public class HangfireService
{
    private ApplicationDbContext DbContext { get; }
    
    private IConfiguration Configuration { get; }
    
    public HangfireService(ApplicationDbContext dbContext, IConfiguration configuration)
    {
        DbContext = dbContext;
        Configuration = configuration;
    }

    public async Task BackupDatabase(int backupJobId)
    {
        var backupJob = await DbContext.BackupJobs
            .Include(b => b.Server)
            .FirstOrDefaultAsync(b => b.Id == backupJobId);
        
        if (backupJob is null)
            throw new Exception($"BackupJob with id {backupJobId} not found");
        
        if(!backupJob.Enabled)
            throw new Exception($"BackupJob '{backupJob.Name}' is not enabled");
        
        if(backupJob.Server is null)
            throw new Exception($"BackupJob '{backupJob.Name}' has no server or server is deleted");

        var backupService = (backupJob.Server.Type switch
        {
            DatabaseTypes.Postgres => new PostgresBackupService(Configuration),
            DatabaseTypes.MySql => new MySqlBackupService(Configuration) as DatabaseBackup,
            _ => throw new Exception($"Server type {backupJob.Server.Type} is not supported")
        }).ForServer(backupJob.Server);

        var databases = backupJob.DatabaseNames.Split(Constants.InputFieldDatabaseNameSeparator);
        
        var listOfErrors = new List<string>();
        
        foreach (var database in databases)
        {
            try
            {
                var backup = await backupService.BackupDatabase(database);
            
                if (backup is null)
                    throw new Exception("Server of service is null");
                
                backup.JobId = backupJob.Id;
                backup.Job = backupJob;
            
                await DbContext.Backups.AddAsync(backup);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                
                listOfErrors.Add($"{database}: {e.Message}");
            }
        }
        
        await DbContext.SaveChangesAsync();
        
        if (listOfErrors.Any())
            throw new Exception($"Backup of certain databases failed: {string.Join(", ", listOfErrors)}");
    }

    public static async Task InitHangfireRecurringJob(ApplicationDbContext dbContext)
    {
        var hangfire = new HangfireService(null, null);
        var jobs = await dbContext.BackupJobs.Where(b => b.Enabled).ToArrayAsync();

        foreach (var job in jobs)
        {
            RecurringJob.AddOrUpdate($"BackupJob-{job.Name}-{job.Id}", () => hangfire.BackupDatabase(job.Id), job.Cron);
        }
    }
}