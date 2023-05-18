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
    
    public string GetFileSize()
    {
        try
        {
            var bytes = (_fileInfo ??= new FileInfo(Path)).Length;
            var suffixes = Constants.FileSizeSuffixes;
            var suffixIndex = 0;
        
            while (bytes > 1024 && suffixIndex < suffixes.Length - 1)
            {
                bytes /= 1024;
                suffixIndex++;
            }
            return $"{bytes} {suffixes[suffixIndex]}";
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);

            return "Unknown";
        }
    }
}