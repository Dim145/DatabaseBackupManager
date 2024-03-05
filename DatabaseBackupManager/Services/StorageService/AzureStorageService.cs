using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using DatabaseBackupManager.Data;
using Minio.Exceptions;

using static DatabaseBackupManager.Data.Seeds;

namespace DatabaseBackupManager.Services.StorageService;

public class AzureStorageService: IStorageService
{
    private BlobServiceClient Client { get; }
    private BlobContainerClient Container { get; set; }
    
    
    private DirectoryInfo TempPath { get; } = Directory.CreateDirectory(Path.Combine(StorageSettings.TempPath, Guid.NewGuid().ToString()));
    

    public AzureStorageService()
    {
        Client = new BlobServiceClient(
            new Uri($"http{(StorageSettings.S3UseSSL ? "s" : "")}://{StorageSettings.S3Endpoint}"),
            new StorageSharedKeyCredential(StorageSettings.AccessKey, StorageSettings.SecretKey)
        );
        
        Container = Client.GetBlobContainerClient(StorageSettings.S3Bucket);
    } 
    
    public async Task<bool> MoveTo(string pathFrom, string pathTo)
    {
        var blob = Container.GetBlobClient(pathTo);
        
        var response = await blob.UploadAsync(pathFrom, true);
        
        if(!response.HasValue)
            throw new ObjectNotFoundException($"Error moving file {pathFrom} to {pathTo}");
        
        File.Delete(pathFrom);
        
        return true;
    }

    public async Task<bool> Delete(string path)
    {
        var response = await  Container.DeleteBlobAsync(path);
        
        return response.Status == 200;
    }

    public Task<string[]> GetFiles(string path)
    {
        var blobs = Container.GetBlobs(BlobTraits.None, BlobStates.None, path);
        
        return Task.FromResult(blobs.Select(b => b.Name).ToArray());
    }

    public async Task<FileInfo> Get(string path)
    {
        var tempFile = Path.Combine(TempPath.FullName, Path.GetFileName(path));
        var blob = Container.GetBlobClient(path);

        var response = await blob.DownloadToAsync(tempFile);
        
        if (response.Status != 200)
            throw new ObjectNotFoundException($"Error downloading file {path}");
        
        return new FileInfo(tempFile);
    }

    public Task<Stream> Download(string path, Action<Stream> callback = null)
    {
        var blob = Container.GetBlobClient(path);
        
        var response = blob.OpenRead();

        callback?.Invoke(response);

        return Task.FromResult(response);
    }

    public async Task<bool> Exists(string path)
    {
        return await Container.GetBlobClient(path).ExistsAsync();
    }

    public async Task<long> GetObjectSize(string path)
    {
        var  blob = Container.GetBlobClient(path);
        var properties = await blob.GetPropertiesAsync();

        if (!properties.HasValue)
            throw new ObjectNotFoundException("object not found.");
        
        return properties.Value.ContentLength;
    }

    public Task<string> DownloadLink(string path, int expirationInMinutes = 60)
    {
        var blob = Container.GetBlobClient(path);

        return Task.FromResult(
            blob.GenerateSasUri(
                BlobSasPermissions.Read, 
                DateTimeOffset.Now.AddMinutes(expirationInMinutes)
                )
                .ToString()
            );
    }
}