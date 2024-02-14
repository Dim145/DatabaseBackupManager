using System.Diagnostics;
using Core.Models;
using Microsoft.Extensions.Configuration;

namespace Core.Services;

public class PostgresBackupService: DatabaseBackup
{
    public override async Task<Backup> BackupDatabase(string databaseName, CancellationToken token = default)
    {
        if (Server is null)
            return null;
        
        var path = GetPathForBackup(databaseName, Constants.PostgresBackupFileExtension);

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "pg_dump",
            Arguments = $"\"host='{Server.Host}' port='{Server.Port}' dbname='{databaseName}' user='{Server.User}' password='{Server.Password}'\" -f {path} -F p",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        });

        await process.WaitForExitAsync(token);
        
        if (process.ExitCode != 0)
            throw new Exception($"pg_dump failed with exit code {process.ExitCode}");

        return new Backup
        {
            BackupDate = DateTime.UtcNow,
            Path = path,
        };
    }

    public override async Task<bool> RestoreDatabase(Backup backup, CancellationToken token = default)
    {
        if (Server is null)
            return false;
        
        var path = GetPathOrUncompressedPath(backup);
        
        var filesParts = Path.GetFileNameWithoutExtension(path)?.Split('_') ?? Array.Empty<string>();
        var databaseName = string.Join("_", filesParts.SkipLast(1));
        
        if (string.IsNullOrEmpty(databaseName))
            return false;
        
        var cmd = $"pg_restore -U {Server.User} -h {Server.Host} -p {Server.Port} -d {databaseName} -c {path}";
        
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{cmd}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true
        });
        
        process.WaitForInputIdle();
        await process.StandardInput.WriteLineAsync(Server.Password);
        
        await process.WaitForExitAsync(token);
        
        if(backup.Compressed && File.Exists(path))
            File.Delete(path);
        
        return process.ExitCode == 0;
    }
}