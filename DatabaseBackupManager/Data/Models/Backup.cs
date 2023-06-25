using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DatabaseBackupManager.Data.Models;

public class Backup: BaseModel
{
    public int JobId { get; set; }
    
    [Required]
    [ForeignKey(nameof(JobId))]
    public BackupJob Job { get; set; }
    
    public DateTime BackupDate { get; set; }
    public string Path { get; set; }
    
    [NotMapped]
    [ValidateNever]
    public bool Compressed => Path.EndsWith(".zip");
    
    [NotMapped]
    [ValidateNever]
    public string FileName => System.IO.Path.GetFileName(Path);
    
    private FileInfo _fileInfo;

    public string GetFileSizeString()
    {
        try
        {
            return GetFileSize().ToSizeString();
        }
        catch (FileNotFoundException)
        {
            return "File not found";
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            
            return "Unknown";
        }
    }
    
    /// <summary>
    ///     Returns the size of the file in bytes
    /// </summary>
    /// <exception cref="FileNotFoundException"></exception>
    /// <returns></returns>
    public long GetFileSize() => (_fileInfo ??= new FileInfo(Path)).Length;
}