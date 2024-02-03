using Core.Models;
using Core.Services;
using Microsoft.Extensions.Configuration;

namespace Core;

public static class Constants
{
    public const string BackupPathAppSettingName = "BackupPath";
    
    public const string PostgresBackupFileExtension = "pgbbak";
    public const string MySqlBackupFileExtension = "sql";
    public const string SqlServerBackupFileExtension = "bak";
    public const string SqliteBackupFileExtension = "sqlitebak";
    
    public static readonly string[] FileSizeSuffixes = { "B", "KB", "MB", "GB", "TB" };

    public const short TimeBeforeTimeoutAgent = 5;
 
    public static string ToSizeString(this long size)
    {
        try
        {
            double bytes = size;
            var suffixIndex = 0;
        
            while (bytes > 1024 && suffixIndex < FileSizeSuffixes.Length - 1)
            {
                bytes /= 1024.0;
                suffixIndex++;
            }
            return $"{bytes:F} {FileSizeSuffixes[suffixIndex]}";
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);

            return "Unknown";
        }
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