namespace DatabaseBackupManager.Models;

public class RedisSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Password { get; set; }
    public int Database { get; set; }
    public bool Ssl { get; set; }
    public int CacheExpiration { get; set; }
    public int Timeout { get; set; }
    public bool LogsEnabled { get; set; }
    public string KeyPrefix { get; set; }
}