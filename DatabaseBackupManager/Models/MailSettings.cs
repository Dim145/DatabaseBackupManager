namespace DatabaseBackupManager.Models;

public class MailSettings
{
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string From { get; set; } = null!;
    public string FromName { get; set; } = null!;
    public bool UseSsl { get; set; }
    
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Host) &&
               !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !string.IsNullOrWhiteSpace(From) &&
               Port is > 0 and < 65536;
    }
    
    public override string ToString()
    {
        return $"Host: {Host}\nPort: {Port}\nUser: {Username}\nPassword: {Password}\nFrom: {From}\nFromName: {FromName}";
    }
}