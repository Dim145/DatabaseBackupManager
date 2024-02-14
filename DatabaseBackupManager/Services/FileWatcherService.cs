using DatabaseBackupManager.Data;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Services;

public class FileWatcherService: BackgroundService
{
    private BaseContext DbContext { get; }
    private IConfiguration Configuration { get; }
    private IServiceScope ServiceScope { get; }
    private FileSystemWatcher Watcher { get; }
    
    private int _errorCount;
    private Task _resetErrorCountTask;
    
    public FileWatcherService(IServiceProvider provider)
    {
        ServiceScope = provider.CreateScope();
        
        DbContext = ServiceScope.ServiceProvider.GetRequiredService<BaseContext>();
        Configuration = ServiceScope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        Watcher = new FileSystemWatcher(Configuration.GetValue<string>(Core.Constants.BackupPathAppSettingName))
        {
            IncludeSubdirectories = true
        };
    }
    
    // don't use async method because this cause server waiting for the task to complete
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () =>
    {
        Watcher.BeginInit();

        while (!stoppingToken.IsCancellationRequested && _errorCount < 5)
        {
            try
            {
                var fileEvent = Watcher.WaitForChanged(WatcherChangeTypes.All, 10_000);

                if (fileEvent.TimedOut)
                    continue;

                switch (fileEvent.ChangeType)
                {
                    case WatcherChangeTypes.Deleted:
                        await HandleDeletedFile(fileEvent);
                        break;
                    case WatcherChangeTypes.Renamed:
                        await HandleRenamedFile(fileEvent);
                        break;
                    default:
                        Console.WriteLine($"FileWatcherService: {fileEvent.Name} was {fileEvent.ChangeType} event");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                _errorCount++;

                if (_resetErrorCountTask != null)
                {
                    _resetErrorCountTask.Dispose();
                    _resetErrorCountTask = null;
                }

                _resetErrorCountTask ??= Task.Run(async () =>
                {
                    await Task.Delay(30_000, stoppingToken);
                    _errorCount = 0;
                    _resetErrorCountTask = null;
                }, stoppingToken);
            }
        }

        Watcher.EndInit();
    }, stoppingToken);
    
    private async Task HandleDeletedFile(WaitForChangedResult fileEvent)
    {
        var backup = await DbContext.Backups
            .Include(b => b.Job)
            .FirstOrDefaultAsync(b => b.Path == fileEvent.Name);
        
        if (backup is null)
            return;
        
        DbContext.Backups.Remove(backup);
        await DbContext.SaveChangesAsync();
        
        Console.WriteLine($"Backup {backup.Path} (#{backup.Id}) was deleted");
    }
    
    private async Task HandleRenamedFile(WaitForChangedResult fileEvent)
    {
        var backup = await DbContext.Backups
            .Include(b => b.Job)
            .FirstOrDefaultAsync(b => b.Path == fileEvent.OldName);
        
        if (backup is null)
            return;
        
        backup.Path = fileEvent.Name;
        await DbContext.SaveChangesAsync();
        
        Console.WriteLine($"Backup {backup.Path} (#{backup.Id}) was renamed");
    }
}