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
    
    public abstract Task<Backup> BackupDatabase(string databaseName, CancellationToken cancellationToken = default);
    public abstract Task<bool> RestoreDatabase(Backup backup, CancellationToken cancellationToken = default);
}