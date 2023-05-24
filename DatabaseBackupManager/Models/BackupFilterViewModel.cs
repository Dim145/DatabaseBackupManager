using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseBackupManager.Models;

[BindProperties]
public class BackupFilterViewModel
{
    [Parameter]
    public int ?ServerId { get; set; }
    [Parameter]
    public int? JobId { get; set; }
    [Parameter]
    public string FileName { get; set; }
    [Parameter]
    public DateTime? Date { get; set; }
    [Parameter]
    public int? FileSize { get; set; }
}