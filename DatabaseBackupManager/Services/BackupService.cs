using DatabaseBackupManager.Data;

namespace DatabaseBackupManager.Services;

public class BackupService
{
    private ApplicationDbContext DbContext { get; }
    
    public BackupService(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }
    
    
}