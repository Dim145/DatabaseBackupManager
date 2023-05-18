using System.Text.RegularExpressions;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Controllers;

[Route("backups")]
public class BackupController: Controller
{
    private ApplicationDbContext DbContext { get; }
    
    public BackupController(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }
    
    [HttpGet]
    public IActionResult Index(BackupFilterViewModel filters, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        
        var query = DbContext.Backups
            .Include(b => b.Job)
            .Include(b => b.Job.Server).AsQueryable();

        // iterate over all properties of the filters object
        foreach (var property in filters.GetType().GetProperties())
        {
            // get the value of the property
            var value = property.GetValue(filters);
            
            // if the value is null, skip this property
            if (value is null)
                continue;
            
            // if the value is a string and it is empty, skip this property
            if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
                continue;

            switch (property.Name)
            {
                case "Server":
                    query = query.Where(b => b.Job.Server.Name.Contains(value.ToString()));
                    break;
                case "JobName":
                    query = query.Where(b => b.Job.Name.Contains(value.ToString()));
                    break;
                case "FileName": 
                    query = query.Where(b => b.Path.Contains(value.ToString()));
                    break;
                case "FileSize":
                    query = query.ToList().Where(b => b.GetFileSize().Contains(value.ToString() ?? string.Empty)).AsQueryable();
                    break;
                case "Date":
                    query = query.Where(b => b.BackupDate.Date == (value as DateTime?));
                    break;
            }
        }

        query = query.OrderByDescending(b => b.BackupDate);
        
        ViewBag.TotalPages = (int) Math.Ceiling((double) query.Count() / pageSize);

        var backups = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        ViewBag.Filters = filters;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        return View(backups);
    }
    
    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(int id)
    {
        var backup = await DbContext.Backups
            .Include(b => b.Job)
            .Include(b => b.Job.Server)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (backup is null)
            return NotFound();

        var file = System.IO.File.OpenRead(backup.Path);
        
        return File(file, "application/octet-stream", backup.FileName);
    }
    
    [HttpGet("delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var backup = await DbContext.Backups
            .Include(b => b.Job)
            .Include(b => b.Job.Server)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (backup is null)
            return NotFound();

        System.IO.File.Delete(backup.Path);
        
        DbContext.Backups.Remove(backup);
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction("Index");
    }
}