namespace DatabaseBackupManager.Services.StorageService;

public interface IStorageService
{
    Task<bool> MoveTo(string pathFrom, string pathTo);
    Task<bool> Delete(string path);

    public Task<string[]> GetFiles(string path);
    Task<FileInfo> Get(string path);
    Task<Stream> Download(string path, Action<Stream> callback = null);
    Task<bool> Exists(string path);
    Task<long> GetObjectSize(string path);
    Task<string> DownloadLink(string path, int expirationInMinutes = 60);
}