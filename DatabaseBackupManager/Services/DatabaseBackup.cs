using System.IO.Compression;
using DatabaseBackupManager.Data.Models;

namespace DatabaseBackupManager.Services;

public abstract class DatabaseBackup
{
    protected Server Server { get; private set; }
    protected string BackupPath { get; }
    
    protected DatabaseBackup(string backupPath, Server server = null)
    {
        Server = server;
        BackupPath = backupPath;
    }

    public DatabaseBackup ForServer(Server server)
    {
        Server = server;
        return this;
    }

    protected string GetPathForBackup(string databaseName, string extension = "sql")
    {
        var date = DateTime.Now;
        var filename = $"{databaseName.Replace(" ", "-")}_{date:yyyyMMddHHmmss}.{extension}";
        var path = Path.Combine(BackupPath, Server.Type.ToString(), Server.Name.Replace(" ", "_"));
        
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        
        return Path.Combine(path, filename);
    }

    protected static string GetPathOrUncompressedPath(Backup backup)
    {
        if (!backup.Compressed)
            return backup.Path;

        using var archive = ZipFile.OpenRead(backup.Path);
        var entry = archive.Entries.FirstOrDefault();
        
        if (entry is null)
            throw new Exception($"No entries found in zip file {backup.Path}");
        
        var path = Path.Combine(Path.GetDirectoryName(backup.Path) ?? string.Empty, entry.Name);
        
        if (File.Exists(path))
            File.Delete(path);
        
        entry.ExtractToFile(path);
        
        return path;
    }
    
    public abstract Task<Backup> BackupDatabase(string databaseName, CancellationToken cancellationToken = default);
    public abstract Task<bool> RestoreDatabase(Backup backup, CancellationToken cancellationToken = default);
}