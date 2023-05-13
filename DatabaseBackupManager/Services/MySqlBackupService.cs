using System.Diagnostics;
using DatabaseBackupManager.Data.Models;

namespace DatabaseBackupManager.Services;

public class MySqlBackupService: DatabaseBackup
{
    public MySqlBackupService(IConfiguration conf, Server server = null) : base(conf.GetValue<string>(Constants.BackupPathAppSettingName), server)
    {
    }

    public override async Task<Backup> BackupDatabase(string databaseName, CancellationToken cancellationToken = default)
    {
        if(Server is null)
            return null;

        var path = GetPathForBackup(databaseName);
        
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
        
        var databaseName = Path.GetFileNameWithoutExtension(backup.Path)?.Split('_')[0];
        
        if (string.IsNullOrEmpty(databaseName))
            return false;
        
        var cmd = $"mysql -u {Server.User} -p{Server.Password} -h {Server.Host} -P {Server.Port} \"{databaseName}\" < \"{backup.Path}\"";
        
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
            throw new Exception($"mysql restore failed with exit code {process.ExitCode}");
        
        return true;
    }
}