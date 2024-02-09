using Core.Models;
using Core.Services;
using Cronos;
using DatabaseBackupManager.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager;

public static class Constants
{
    public const string InputFieldDatabaseNameSeparator = ",";
    public const string DayBeforeCompressionName = "dayBeforeCompression";
    public const string CronForCompressionJobName = "cronForCompressionJob";

    public const string DefaultValueForSqliteColumns = "sqlite_default";

    public static readonly string[] AllBackupsFileExtensions = { Core.Constants.PostgresBackupFileExtension, Core.Constants.MySqlBackupFileExtension, Core.Constants.SqlServerBackupFileExtension, Core.Constants.SqliteBackupFileExtension };

    internal static void AddOrUpdateHangfireJob(BackupJob backupJob)
    {
        AddOrUpdateHangfireJob(GetJobNameForBackupJob(backupJob), backupJob.Id, backupJob.Cron);
    }

    internal static void AddOrUpdateHangfireJob(string jobName, int jobId, string cron)
    {
        HangfireService pseudoHangfireContext = new(null, null, null);
        
        RecurringJob.AddOrUpdate(jobName, () => pseudoHangfireContext.BackupDatabase(jobId), cron);
    }
    
    internal static void RemoveBackupJobFromHangfire(BackupJob backupJob)
    {
        RecurringJob.RemoveIfExists(GetJobNameForBackupJob(backupJob));
    }
    
    internal static string GetJobNameForBackupJob(BackupJob backupJob)
    {
        return GetJobNameForBackupJob(backupJob.Name, backupJob.Id);
    }
    
    internal static string GetJobNameForBackupJob(string jobName, int backupJobId)
    {
        return $"BackupJob-{jobName}-{backupJobId}";
    }

    internal static bool TryParseCron(this string cron, out CronExpression expression, CronFormat? format = null)
    {
        try
        {
            expression = CronExpression.Parse(cron, format ?? CronFormat.Standard);
            return expression != null;
        }
        catch (Exception)
        {
            expression = null;
            return false;
        }
    }
    
    internal static IEnumerable<BackupJob> IncludeServer(this IQueryable<BackupJob> backupJobs)
    {
        return backupJobs.ToList().Select(job =>
        {
            job.Server = job.ServerType switch
            {
                nameof(Server) => backupJobs.Provider.CreateQuery<Server>(backupJobs.Expression)
                    .FirstOrDefault(s => s.Id == job.ServerId),
                nameof(Agent) => backupJobs.Provider.CreateQuery<Agent>(backupJobs.Expression)
                    .FirstOrDefault(s => s.Id == job.ServerId),
                _ => null
            };
            
            return job;
        });
    }
}