namespace DatabaseBackupManager.Models;

public class MailSettings
{
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string User { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string From { get; set; } = null!;
    public string FromName { get; set; } = null!;
    
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Host) &&
               !string.IsNullOrWhiteSpace(User) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !string.IsNullOrWhiteSpace(From) &&
               Port is > 0 and < 65536;
    }
}