using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DatabaseBackupManager.Models;

namespace DatabaseBackupManager.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        ViewBag.Drives = DriveInfo.GetDrives()
            .Where(d => Directory.Exists(d.VolumeLabel) || Directory.Exists(d.Name))
            .Where(d => d.TotalSize > 0)
            .Where(d => d.DriveType is DriveType.Fixed or DriveType.Removable)
            .Where(d => d.IsReady)
            .ToArray();
        
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