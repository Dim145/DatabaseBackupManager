using System.Diagnostics;
using DatabaseBackupManager.Data.Models;

namespace DatabaseBackupManager.Services;

public class PostgresBackupService: IDatabaseBackup
{
    private Server Server { get; set; }
    private string BackupPath { get; }
    
    public PostgresBackupService(Server server, IConfiguration conf): this(conf)
    {
        Server = server;
    }

    public PostgresBackupService(IConfiguration conf)
    {
        BackupPath = conf.GetValue<string>("BackupPath");
        
        if (string.IsNullOrEmpty(BackupPath))
            throw new Exception("BackupPath is not set in appsettings.json");
    }

    public IDatabaseBackup ForServer(Server server)
    {
        Server = server;
        return this;
    }

    public async Task<Backup> BackupDatabase(string databaseName, CancellationToken token = default)
    {
        if (Server is null)
            return null;
        
        var date = DateTime.Now;
        var filename = $"{databaseName}_{date:yyyyMMddHHmmss}.pg_backup";
        var path = Path.Combine(BackupPath, Server.Type.ToString(), Server.Name, filename);

        var cmd = $"pg_dump -U {Server.User} -h {Server.Host} -p {Server.Port} -d {databaseName} -f {path} -F p";
        
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{cmd}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        process.WaitForInputIdle();
        await process.StandardInput.WriteLineAsync(Server.Password);
        
        await process.WaitForExitAsync(token);
        
        if (process.ExitCode != 0)
            throw new Exception($"pg_dump failed with exit code {process.ExitCode}");

        return new Backup
        {
            BackupDate = date,
            Path = path,
        };
    }

    public async Task<bool> RestoreDatabase(Backup backup, CancellationToken token = default)
    {
        if (Server is null)
            return false;
        
        var databaseName = Path.GetFileNameWithoutExtension(backup.Path)?.Split('_')[0];
        
        if (string.IsNullOrEmpty(databaseName))
            return false;
        
        var cmd = $"pg_restore -U {Server.User} -h {Server.Host} -p {Server.Port} -d {databaseName} -c {backup.Path}";
        
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
        
        return process.ExitCode == 0;
    }
}