namespace DatabaseBackupManager.Services.StorageService;

public class LocalStorageService(IConfiguration conf) : IStorageService
{
    private string BasePath { get; } = conf.GetValue<string>(Core.Constants.BackupPathAppSettingName);

    public Task<bool> MoveTo(string pathFrom, string pathTo)
    {
        if (!File.Exists(pathFrom))
            throw new Exception($"File {pathFrom} does not exist");
        
        pathTo = Path.Combine(BasePath, pathTo);
        
        if (File.Exists(pathTo))
            File.Delete(pathTo);
        
        File.Move(pathFrom, pathTo);
        
        return Task.FromResult(true);
    }
    
    public Task<bool> Delete(string path)
    {
        path = Path.Combine(BasePath, path);
        
        if (File.Exists(path))
            File.Delete(path);
        
        return Task.FromResult(true);
    }
    
    public Task<string[]> GetFiles(string path)
    {
        path = Path.Combine(BasePath, path);
        
        if (!Directory.Exists(path))
            throw new Exception($"Directory {path} does not exist");
        
        return Task.FromResult(Directory.GetFiles(path,  "*.*", new EnumerationOptions
        {
            RecurseSubdirectories = true,
            MaxRecursionDepth = 3,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.System
        }));
    }
    
    public Task<FileInfo> Get(string path)
    {
        path = Path.Combine(BasePath, path);
        
        if (!File.Exists(path))
            throw new Exception($"File {path} does not exist");
        
        return Task.FromResult(new FileInfo(path));
    }

    public Task<Stream> Download(string path, Action<Stream> callback = null)
    {
        path = Path.Combine(BasePath, path);
        
        if (!File.Exists(path))
            throw new Exception($"File {path} does not exist");

        return Task.FromResult<Stream>(File.OpenRead(path));
    }

    public Task<string> DownloadLink(string path, int expirationInMinutes = 0)
    {
        return Task.FromResult(Path.Combine(BasePath, path));
    }

    public Task<bool> Exists(string path)
    {
        return Task.FromResult(File.Exists(Path.Combine(BasePath, path)));
    }
    
    public Task<long> GetObjectSize(string path)
    {
        return Task.FromResult(new FileInfo(Path.Combine(BasePath, path)).Length);
    }
}