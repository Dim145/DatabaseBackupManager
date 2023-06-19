namespace DatabaseBackupManager.Models;

public class DataSettings
{
    public string DefaultAdminRole { get; set; }
    public string DefaultAdminEmail { get; set; }
    public string DefaultAdminPassword { get; set; }
    
    public string DefaultReaderRole { get; set; }
    public string DefaultEditorRole { get; set; }
    public string DefaultRestorerRole { get; set; }
}