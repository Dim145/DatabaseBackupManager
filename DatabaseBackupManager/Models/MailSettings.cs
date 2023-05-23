namespace DatabaseBackupManager.Models;

public class MailSettings
{
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string User { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string From { get; set; } = null!;
    public string FromName { get; set; } = null!;
}