using System.Globalization;
using System.IO.Compression;
using Core;
using Core.Models;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Services.StorageService;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DatabaseBackupManager.Services;

public class HangfireService(
    ApplicationDbContext dbContext,
    IConfiguration configuration,
    IStorageService storageService)
{
    private ApplicationDbContext DbContext { get; } = dbContext;
    private IConfiguration Configuration { get; } = configuration;
    private IStorageService StorageService { get; } = storageService;

    [AutomaticRetry(Attempts = 1, DelaysInSeconds = [30])]
    public async Task BackupDatabase(int backupJobId)
    {
        var backupJob = await DbContext.BackupJobs
            .FirstOrDefaultAsync(b => b.Id == backupJobId);
        
        if (backupJob is null)
            throw new Exception($"BackupJob with id {backupJobId} not found");

        BackgroundJob.Enqueue(() => CleanBackupRep(backupJobId));
        
        if(!backupJob.Enabled)
            throw new Exception($"BackupJob '{backupJob.Name}' is not enabled");

        switch (backupJob.ServerType)
        {
            case nameof(Server):
                var server = await DbContext.Servers.FirstOrDefaultAsync(s => s.Id == backupJob.ServerId);
                
                if(server is null)
                    throw new Exception($"BackupJob '{backupJob.Name}' has no server, server is deleted or not of type Server");
                
                backupJob.Server = server;

                var backupService = backupJob.Server.Type.GetService().ForServer(server);

                var databases = backupJob.DatabaseNames.Split(Constants.InputFieldDatabaseNameSeparator);
        
                var listOfErrors = new List<string>();
        
                foreach (var database in databases)
                {
                    try
                    {
                        var backup = await backupService.BackupDatabase(database);
            
                        if (backup is null)
                            throw new Exception("Server of service is null");
                        
                        backup.Size = new FileInfo(backup.Path).Length;
                        
                        // backup is in tmp file, move it to storage
                        var newPath = backup.Path[(Core.Constants.TempDirForBackups.Length + 1)..];
                        await StorageService.MoveTo(backup.Path, newPath);
                        
                        if (File.Exists(backup.Path))
                            File.Delete(backup.Path);
                
                        backup.JobId = backupJob.Id;
                        backup.Job = backupJob;
                        backup.Path = newPath;
            
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
                break;
            
            case nameof(Agent):
                
                // todo call agent  and wait  for result (with singleton service ?)
                
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(backupJob.ServerType), backupJob.ServerType,
                    "ServerType is not supported");
        }
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new []{ 10, 30, 60 })]
    public async Task CleanBackupRep(int backupJobId)
    {
        var backupJob = await DbContext.BackupJobs
            .Include(b => b.Backups)
            .FirstOrDefaultAsync(b => b.Id == backupJobId);
        
        if (backupJob is null)
            throw new Exception($"BackupJob with id {backupJobId} not found");
        
        foreach (var backup in backupJob.Backups?.Where(b => DateTime.UtcNow - b.BackupDate > backupJob.Retention) ?? ArraySegment<Backup>.Empty)
        {
            DbContext.Backups.Remove(backup);
        }
        
        await DbContext.SaveChangesAsync();
    }

    // only compress files in local storage.
    // todo: add support for remote storage compression ? or just compress at the time of moving to remote storage ?
    public async Task<string> CompressFileIfNeeded()
    {
        var dayBeforeCompression = TimeSpan.FromDays(Configuration.GetValue<int>(Constants.DayBeforeCompressionName));
        var compressDay = DateTime.UtcNow - dayBeforeCompression;
        
        var backupsToCompress = await DbContext.Backups
            .Where(b => b.BackupDate < compressDay)
            .ToArrayAsync();
        
        backupsToCompress = backupsToCompress
            .Where(b => !b.Compressed)
            .ToArray();

        var listOdRes = new List<string>();

        foreach (var backup in backupsToCompress)
        {
            var compressedFileName = $"{backup.FileName}.zip";
            
            var tempFile = await StorageService.Get(backup.Path);
            
            if (tempFile is null)
                continue;
            
            compressedFileName = Path.GetDirectoryName(tempFile.FullName) + compressedFileName;

            using (var zipFile = ZipFile.Open(compressedFileName, ZipArchiveMode.Create))
            {
                zipFile.CreateEntryFromFile(tempFile.FullName, $"{Path.GetFileName(backup.Path)}");
            }
            
            var compressedSize = new FileInfo(compressedFileName).Length;
            
            tempFile.Delete();
            await StorageService.MoveTo(compressedFileName, backup.Path + ".zip");
            await StorageService.Delete(backup.Path);
            
            backup.Path += ".zip";
            backup.Size = compressedSize;
            
            listOdRes.Add($"backup {backup.Path} compressed => {compressedFileName}");
        }
        
        await DbContext.SaveChangesAsync();
        
        if(listOdRes.IsNullOrEmpty())
            listOdRes.Add("No files are compressed");

        return string.Join("\n", listOdRes);
    }

    public static async Task InitHangfireRecurringJob(ApplicationDbContext dbContext, IConfiguration conf)
    {
        var hangfire = new HangfireService(null, null, null);
        var jobs = await dbContext.BackupJobs.Where(b => b.Enabled).ToArrayAsync();

        foreach (var job in jobs)
        {
            RecurringJob.AddOrUpdate($"BackupJob-{job.Name}-{job.Id}", () => hangfire.BackupDatabase(job.Id), job.Cron);
        }
        
        var cron = conf.GetValue<string>(Constants.CronForCompressionJobName);
        
        RecurringJob.AddOrUpdate("Compress backups", () => hangfire.CompressFileIfNeeded(), cron);
    }
}