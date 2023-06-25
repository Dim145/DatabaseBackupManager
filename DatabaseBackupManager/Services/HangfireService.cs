using System.Globalization;
using System.IO.Compression;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Data.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

    [AutomaticRetry(Attempts = 1, DelaysInSeconds = new []{ 30 })]
    public async Task BackupDatabase(int backupJobId)
    {
        var backupJob = await DbContext.BackupJobs
            .Include(b => b.Server)
            .FirstOrDefaultAsync(b => b.Id == backupJobId);
        
        if (backupJob is null)
            throw new Exception($"BackupJob with id {backupJobId} not found");

        BackgroundJob.Enqueue(() => CleanBackupRep(backupJobId));
        
        if(!backupJob.Enabled)
            throw new Exception($"BackupJob '{backupJob.Name}' is not enabled");
        
        if(backupJob.Server is null)
            throw new Exception($"BackupJob '{backupJob.Name}' has no server or server is deleted");

        var backupService = backupJob.Server.Type.GetService(Configuration).ForServer(backupJob.Server);

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

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new []{ 10, 30, 60 })]
    public async Task CleanBackupRep(int backupJobId)
    {
        var backupJob = await DbContext.BackupJobs
            .Include(b => b.Server)
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

    public async Task<string> CompressFileIfNeeded()
    {
        var dayBeforeCompression = TimeSpan.FromDays(Configuration.GetValue<int>(Constants.DayBeforeCompressionName));
        var backupRoot = Configuration.GetValue<string>(Constants.BackupPathAppSettingName);
        
        var files = Directory.GetFiles(backupRoot, "*.*", new EnumerationOptions
        {
            RecurseSubdirectories = true,
            MaxRecursionDepth = 3,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.System
        });
        
        files = files.Where(f => Constants.AllBackupsFileExtensions.Contains(Path.GetExtension(f)[1..])).ToArray();

        var listOdRes = new List<string>();

        foreach (var file in files)
        {
            var originalFileName = Path.GetFileNameWithoutExtension(file);
            var fileNameParts = originalFileName.Split('_');
            
            if(fileNameParts.Length < 2)
                continue;
            
            var fileDate = DateTime.ParseExact(fileNameParts.Last(), "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            
            if(DateTime.UtcNow - fileDate < dayBeforeCompression)
                continue;
            
            var compressedFileName = Path.Combine(Path.GetDirectoryName(file)!, $"{originalFileName}.zip");
            
            if (File.Exists(Path.Combine(backupRoot, compressedFileName)))
                continue;
            
            using var zipFile = ZipFile.Open(Path.Combine(backupRoot, compressedFileName), ZipArchiveMode.Create);
            zipFile.CreateEntryFromFile(file, Path.GetFileName(file));
            
            File.Delete(file);
            
            listOdRes.Add($"backup {originalFileName} compressed => {compressedFileName}");

            var backup = await DbContext.Backups.FirstOrDefaultAsync(b => b.Path == file);
            
            if (backup is null)
                continue;
            
            backup.Path = compressedFileName;
        }
        
        await DbContext.SaveChangesAsync();
        
        if(listOdRes.IsNullOrEmpty())
            listOdRes.Add("No files are compressed");

        return string.Join("\n", listOdRes);
    }

    public static async Task InitHangfireRecurringJob(ApplicationDbContext dbContext, IConfiguration conf)
    {
        var hangfire = new HangfireService(null, null);
        var jobs = await dbContext.BackupJobs.Where(b => b.Enabled).ToArrayAsync();

        foreach (var job in jobs)
        {
            RecurringJob.AddOrUpdate($"BackupJob-{job.Name}-{job.Id}", () => hangfire.BackupDatabase(job.Id), job.Cron);
        }
        
        var cron = conf.GetValue<string>(Constants.CronForCompressionJobName);
        
        RecurringJob.AddOrUpdate("Compress backups", () => hangfire.CompressFileIfNeeded(), cron);
    }
}