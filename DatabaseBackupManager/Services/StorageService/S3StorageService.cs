using System.Globalization;
using System.Reactive.Linq;
using DatabaseBackupManager.Data;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Encryption;
using Minio.DataModel.ObjectLock;
using NuGet.Common;
using static DatabaseBackupManager.Data.Seeds;

namespace DatabaseBackupManager.Services.StorageService;

public class S3StorageService(IMinioClient minioClient) : IStorageService
{
    private IMinioClient MinioClient { get; } = minioClient;

    private DirectoryInfo TempPath { get; } = Directory.CreateDirectory(Path.Combine(StorageSettings.TempPath, Guid.NewGuid().ToString()));
    
    public async Task<bool> MoveTo(string pathFrom, string pathTo)
    {
        if (!File.Exists(pathFrom))
        {
            var getFileInfo = await Get(pathFrom);
            
            File.Move(getFileInfo.FullName, pathTo);
            
            await Delete(pathFrom);
            
            return true;
        }

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(StorageSettings.S3Bucket)
            .WithObject(pathTo)
            .WithFileName(pathFrom)
            // database backup content type
            .WithContentType("application/x-sql");
        
        if(StorageSettings.ServerSideEncryption != null)
            putObjectArgs.WithServerSideEncryption(StorageSettings.ServerSideEncryption);
        
        if(StorageSettings.S3DaysRetention.HasValue)
            putObjectArgs.WithRetentionConfiguration(new ObjectRetentionConfiguration
            {
                Mode = ObjectRetentionMode.COMPLIANCE,
                RetainUntilDate = DateTime.UtcNow.AddDays(StorageSettings.S3DaysRetention.Value).ToString(CultureInfo.InvariantCulture)
            });
        
        var response = await MinioClient.PutObjectAsync(putObjectArgs);
        
        if (response is null)
            throw new Exception($"Error moving file {pathFrom} to {pathTo}");
        
        File.Delete(pathFrom);
        
        return true;
    }

    public async Task<bool> Delete(string path)
    {
        var removeObjectArgs = new RemoveObjectArgs()
            .WithBucket(StorageSettings.S3Bucket)
            .WithObject(path);
        
        await MinioClient.RemoveObjectAsync(removeObjectArgs);
        
        return true;
    }

    public async Task<string[]> GetFiles(string path)
    {
        var listObjectsArgs = new ListObjectsArgs()
            .WithBucket(StorageSettings.S3Bucket)
            .WithPrefix(path);
        
        var sObservable = MinioClient.ListObjectsAsync(listObjectsArgs);

        var list = await sObservable.ToList();

        return list
            .Where(i => !i.IsDir)
            .Select(i => $"{path}/{i.Key}")
            .ToArray();
    }

    public async Task<FileInfo> Get(string path)
    {
        var tempFile = Path.Combine(TempPath.FullName, Path.GetFileName(path));
        
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(StorageSettings.S3Bucket)
            .WithObject(path)
            .WithFile(tempFile);
        
        await MinioClient.GetObjectAsync(getObjectArgs);
        
        return new FileInfo(tempFile);
    }

    public async Task<Stream> Download(string path, Action<Stream> callback = null)
    {
        Stream stream = new MemoryStream();
        var finished = false;
        
        var isInternal = callback is null;
        
        callback ??= s =>
        {
            s.CopyTo(stream);
            finished = true;
        };

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(StorageSettings.S3Bucket)
            .WithObject(path)
            .WithCallbackStream(callback);
        
        await MinioClient.GetObjectAsync(getObjectArgs);

        if (!isInternal) 
            return stream;
        
        while (!finished)
            await Task.Delay(100);
            
        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }
    
    public async Task<string> DownloadLink(string path, int expirationInMinutes = 60)
    {
        var presignedGetObjectArgs = new PresignedGetObjectArgs()
            .WithBucket(StorageSettings.S3Bucket)
            .WithObject(path)
            .WithExpiry(expirationInMinutes);
        
        return await MinioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
    }
    
    private Task<ObjectStat> GetObjectStat(string path)
    {
        var statObjectArgs = new StatObjectArgs()
            .WithBucket(StorageSettings.S3Bucket)
            .WithObject(path);
        
        return MinioClient.StatObjectAsync(statObjectArgs);
    }

    public async Task<bool> Exists(string path)
    {
        try
        {
            var objectStats = await GetObjectStat(path);
            return objectStats is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    public async Task<long> GetObjectSize(string path)
    {
        var objectStats = await GetObjectStat(path);
        return objectStats.Size;
    }
    
    // override finalizer only if 'Dispose' method is not implemented
    ~S3StorageService()
    {
        TempPath.Delete(true);
    }
}