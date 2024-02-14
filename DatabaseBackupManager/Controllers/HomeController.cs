using System.Diagnostics;
using Core;
using DatabaseBackupManager.Data;
using Microsoft.AspNetCore.Mvc;
using DatabaseBackupManager.Models;
using Microsoft.EntityFrameworkCore;
using Minio;

namespace DatabaseBackupManager.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private BaseContext DbContext { get; }

    public HomeController(ILogger<HomeController> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        DbContext = serviceProvider.GetRequiredService<BaseContext>();
    }

    public IActionResult Index()
    {
        switch (Seeds.StorageSettings.StorageType)
        {
            case "S3":
                ViewBag.Drives = new Dictionary<string, string>[]
                {
                    new()
                    {
                        { "Name", Seeds.StorageSettings.S3Bucket },
                        { "Info", "" },
                        { "Total", "Unlimited" },
                        { "Used", DbContext.Backups.Sum(b => b.Size).ToString() }
                    }
                };
                break;
            default:
                ViewBag.Drives = DriveInfo.GetDrives()
                    .Where(d => Directory.Exists(d.VolumeLabel) || Directory.Exists(d.Name))
                    .Where(d =>
                    {
                        try
                        {
                            return d.TotalSize > 0;
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e);

                            return false;
                        }
                    })
                    .Where(d => d.DriveType is DriveType.Fixed or DriveType.Removable)
                    .Where(d => d.IsReady)
                    .Select(d => new Dictionary<string, string>
                    {
                        { "Name", d.VolumeLabel },
                        { "Info", d.VolumeLabel != d.Name ? $"{d.Name} ({d.DriveFormat})" : d.DriveFormat },
                        { "Total", d.TotalSize.ToString() },
                        { "Used", (d.TotalSize - d.AvailableFreeSpace).ToString() }
                    })
                    .ToArray();
                break;
        }
        
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}