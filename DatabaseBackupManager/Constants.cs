using DatabaseBackupManager.Data.Models;
using DatabaseBackupManager.Services;
using Hangfire;

namespace DatabaseBackupManager;

public static class Constants
{
    public const string InputFieldDatabaseNameSeparator = ",";
    public const string BackupPathAppSettingName = "BackupPath";
    
    internal static void AddOrUpdateHangfireJob(BackupJob backupJob)
    {
        AddOrUpdateHangfireJob(GetJobNameForBackupJob(backupJob), backupJob.Id, backupJob.Cron);
    }

    internal static void AddOrUpdateHangfireJob(string jobName, int jobId, string cron)
    {
        HangfireService pseudoHangfireContext = null;
        
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
}