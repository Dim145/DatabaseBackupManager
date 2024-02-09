using Minio.DataModel.Encryption;

namespace DatabaseBackupManager.Models;

public class StorageSettings
{
    public string TempPath { get; set; }
    public string S3Bucket { get; set; }
    public string S3Region { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string S3Endpoint { get; set; }
    public bool S3UseSSL { get; set; }
    public IServerSideEncryption ServerSideEncryption { get; set; }
    public int? S3DaysRetention { get; set; }
    public string StorageType { get; set; }
    public int S3LinkExpiration { get; set; }
}