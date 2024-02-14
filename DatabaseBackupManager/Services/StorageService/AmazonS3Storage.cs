using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Models;

namespace DatabaseBackupManager.Services.StorageService;

internal class AmazonS3Storage: IStorageService
{
    private AmazonS3Client Client { get; }
    private DirectoryInfo TempPath { get; } = Directory.CreateDirectory(Path.Combine(Seeds.StorageSettings.TempPath, Guid.NewGuid().ToString()));
    
    public AmazonS3Storage()
    {
        Client = new AmazonS3Client(Seeds.StorageSettings.AccessKey, Seeds.StorageSettings.SecretKey, new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(Seeds.StorageSettings.S3Region),
            ServiceURL = Seeds.StorageSettings.S3Endpoint,
            UseHttp = !Seeds.StorageSettings.S3UseSSL
        });
    }
    
    public async Task<bool> MoveTo(string pathFrom, string pathTo)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = Seeds.StorageSettings.S3Bucket,
            Key = pathTo,
            FilePath = pathFrom
        };

        var response = await Client.PutObjectAsync(putRequest);
        
        if (response.HttpStatusCode != HttpStatusCode.OK)
            throw new Exception($"Error moving file {pathFrom} to {pathTo}");
        
        File.Delete(pathFrom);
        
        return true;
    }

    public async Task<bool> Delete(string path)
    {
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = Seeds.StorageSettings.S3Bucket,
            Key = path
        };
        
        var response = await Client.DeleteObjectAsync(deleteRequest);
        
        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    public async Task<string[]> GetFiles(string path)
    {
        var listRequest = new ListObjectsV2Request
        {
            BucketName = Seeds.StorageSettings.S3Bucket,
            Prefix = path
        };
        
        var response = await Client.ListObjectsV2Async(listRequest);
        
        return response.S3Objects.Select(x => x.Key).ToArray();
    }

    public async Task<FileInfo> Get(string path)
    {
        var tempFile = Path.Combine(TempPath.FullName, Path.GetFileName(path));
        
        var getRequest = new GetObjectRequest
        {
            BucketName = Seeds.StorageSettings.S3Bucket,
            Key = path
        };
        
        var response = await Client.GetObjectAsync(getRequest);
        
        await using var fileStream = File.Create(tempFile);
        
        await response.ResponseStream.CopyToAsync(fileStream);
        
        return new FileInfo(tempFile);
    }

    public async Task<Stream> Download(string path, Action<Stream> callback = null)
    {
        Stream stream = new MemoryStream();
        
        var getRequest = new GetObjectRequest
        {
            BucketName = Seeds.StorageSettings.S3Bucket,
            Key = path
        };
        
        var response = await Client.GetObjectAsync(getRequest);
        
        if (callback is null)
        {
            await response.ResponseStream.CopyToAsync(stream);
        }
        else
        {
            callback(response.ResponseStream);
        }
        
        return stream;
    }

    public async Task<bool> Exists(string path)
    {
        var getRequest = new GetObjectMetadataRequest
        {
            BucketName = Seeds.StorageSettings.S3Bucket,
            Key = path
        };
        
        try
        {
            var response = await Client.GetObjectMetadataAsync(getRequest);
            return response.HttpStatusCode == HttpStatusCode.OK;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<long> GetObjectSize(string path)
    {
        var getRequest = new GetObjectMetadataRequest
        {
            BucketName = Seeds.StorageSettings.S3Bucket,
            Key = path
        };
        
        var response = await Client.GetObjectMetadataAsync(getRequest);
        
        return response.ContentLength;
    }

    public Task<string> DownloadLink(string path, int expirationInMinutes = 60)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = Seeds.StorageSettings.S3Bucket,
            Key = path,
            Expires = DateTime.Now.AddMinutes(expirationInMinutes)
        };
        
        return Task.FromResult(Client.GetPreSignedURL(request));
    }
    
    ~AmazonS3Storage()
    {
        TempPath.Delete(true);
    }
}