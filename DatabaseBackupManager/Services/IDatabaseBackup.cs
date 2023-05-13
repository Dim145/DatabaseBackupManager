using DatabaseBackupManager.Data.Models;

namespace DatabaseBackupManager.Services;

public interface IDatabaseBackup
{
    IDatabaseBackup ForServer(Server server);
    Task<Backup> BackupDatabase(string databaseName, CancellationToken cancellationToken = default);
    Task<bool> RestoreDatabase(Backup backup, CancellationToken cancellationToken = default);
}