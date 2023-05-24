using DatabaseBackupManager.Data.Models;
using DatabaseBackupManager.Models;
using DatabaseBackupManager.Services;
using Hangfire;

namespace DatabaseBackupManager;

public static class Constants
{
    public const string InputFieldDatabaseNameSeparator = ",";
    public const string BackupPathAppSettingName = "BackupPath";
    public const string DayBeforeCompressionName = "dayBeforeCompression";
    public const string CronForCompressionJobName = "cronForCompressionJob";

    public const string DefaultValueForSqliteColumns = "sqlite_default";
    
    public static readonly string[] FileSizeSuffixes = { "B", "KB", "MB", "GB", "TB" };
    
    public const string PostgresBackupFileExtension = "pgbbak";
    public const string MySqlBackupFileExtension = "sql";
    public const string SqlServerBackupFileExtension = "bak";
    public const string SqliteBackupFileExtension = "sqlitebak";
    
    public static readonly string[] AllBackupsFileExtensions = { PostgresBackupFileExtension, MySqlBackupFileExtension, SqlServerBackupFileExtension, SqliteBackupFileExtension };

    internal static void AddOrUpdateHangfireJob(BackupJob backupJob)
    {
        AddOrUpdateHangfireJob(GetJobNameForBackupJob(backupJob), backupJob.Id, backupJob.Cron);
    }

    internal static void AddOrUpdateHangfireJob(string jobName, int jobId, string cron)
    {
        HangfireService pseudoHangfireContext = new(null, null);
        
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

    public static DatabaseBackup GetService(this DatabaseTypes type, IConfiguration conf) => type switch
    {
        DatabaseTypes.Postgres => new PostgresBackupService(conf),
        DatabaseTypes.MySql => new MySqlBackupService(conf),
        DatabaseTypes.SqlServer => new SqlServerBackupService(conf),
        DatabaseTypes.Sqlite => new SqliteBackupService(conf),
        _ => throw new Exception($"Server type {type} is not supported")
    };
}