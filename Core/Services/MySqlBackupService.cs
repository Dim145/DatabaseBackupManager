using System.Diagnostics;
using Core.Models;
using Microsoft.Extensions.Configuration;

namespace Core.Services;

public class MySqlBackupService: DatabaseBackup
{
    public override async Task<Backup> BackupDatabase(string databaseName, CancellationToken cancellationToken = default)
    {
        if(Server is null)
            return null;

        var path = GetPathForBackup(databaseName, Constants.MySqlBackupFileExtension);

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "mysqldump",
            Arguments = $"-u {Server.User} -p{Server.Password} -h {Server.Host} -P {Server.Port} \"{databaseName}\" --add-locks --lock-tables --result-file=\"{path}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            
            throw new Exception($"mysqldump failed with exit code {process.ExitCode}: {error}");
        }

        return new Backup
        {
            BackupDate = DateTime.UtcNow,
            Path = path,
        };
    }

    public override async Task<bool> RestoreDatabase(Backup backup, CancellationToken cancellationToken = default)
    {
        if (Server is null)
            return false;

        var path = GetPathOrUncompressedPath(backup);

        var filesParts = Path.GetFileNameWithoutExtension(path)?.Split('_') ?? Array.Empty<string>();
        var databaseName = string.Join("_", filesParts.SkipLast(1));
        
        if (string.IsNullOrEmpty(databaseName))
            return false;
        
        var cmd = $"mysql -u {Server.User} -p{Server.Password} -h {Server.Host} -P {Server.Port} \"{databaseName}\" < \"{path}\"";
        
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{cmd}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        
        await process.WaitForExitAsync(cancellationToken);
        
        if(backup.Compressed && File.Exists(path))
            File.Delete(path);

        if (process.ExitCode != 0)
            throw new Exception($"mysql restore failed with exit code {process.ExitCode}");
        
        return true;
    }
}