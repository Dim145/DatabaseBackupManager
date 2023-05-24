using System.Diagnostics;
using DatabaseBackupManager.Data.Models;

namespace DatabaseBackupManager.Services;

public class MySqlBackupService: DatabaseBackup
{
    public MySqlBackupService(IConfiguration conf, Server server = null) : base(conf.GetValue<string>(Constants.BackupPathAppSettingName), server)
    {
        if (string.IsNullOrEmpty(BackupPath))
            throw new Exception($"{Constants.BackupPathAppSettingName} is not set in appsettings.json");
    }

    public override async Task<Backup> BackupDatabase(string databaseName, CancellationToken cancellationToken = default)
    {
        if(Server is null)
            return null;

        var path = GetPathForBackup(databaseName, Constants.MySqlBackupFileExtension);
        
        var cmd = $"mysqldump -u {Server.User} -p{Server.Password} -h {Server.Host} -P {Server.Port} \"{databaseName}\" --add-locks --lock-tables --result-file=\"{path}\"";
        
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

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            
            throw new Exception($"mysqldump failed with exit code {process.ExitCode}: {error}");
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