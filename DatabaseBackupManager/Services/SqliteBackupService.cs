using System.Diagnostics;
using DatabaseBackupManager.Data.Models;

namespace DatabaseBackupManager.Services;

public class SqliteBackupService: DatabaseBackup
{
    public SqliteBackupService(IConfiguration conf, Server server = null) : base(conf.GetValue<string>(Constants.BackupPathAppSettingName), server)
    {
        if (string.IsNullOrEmpty(BackupPath))
            throw new Exception($"{Constants.BackupPathAppSettingName} is not set in appsettings.json");
    }

    public override async Task<Backup> BackupDatabase(string databaseName, CancellationToken cancellationToken = default)
    {
        if (Server is null)
            return null;

        var path = GetPathForBackup(Path.GetFileNameWithoutExtension(Server.Host), Constants.SqliteBackupFileExtension);

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "sqlite3",
            Arguments = $"{Server.Host} \".backup {path}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        
        await process.WaitForExitAsync(cancellationToken);
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            
            throw new Exception($"sqlite3 backup failed with exit code {process.ExitCode}: {error}");
        }
        
        return new Backup
        {
            BackupDate = DateTime.Now,
            Path = path,
        };
    }

    public override async Task<bool> RestoreDatabase(Backup backup, CancellationToken cancellationToken = default)
    {
        if (Server is null)
            return false;
        
        var path = GetPathOrUncompressedPath(backup);

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "sqlite3",
            Arguments = $"{Server.Host} \".restore {path}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        
        await process.WaitForExitAsync(cancellationToken);
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            
            throw new Exception($"sqlite3 restore failed with exit code {process.ExitCode}: {error}");
        }
        
        return true;
    }
}