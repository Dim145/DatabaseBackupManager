using Core;
using Core.Models;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Middleware;
using DatabaseBackupManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Controllers;

[Route("backups")]
[Authorize(Policy = nameof(Policies.ReaderRolePolicy))]
public class BackupController: Controller
{
    private ApplicationDbContext DbContext { get; }
    private IConfiguration Configuration { get; }
    
    public BackupController(ApplicationDbContext dbContext, IConfiguration configuration)
    {
        DbContext = dbContext;
        Configuration = configuration;
    }
    
    [HttpGet]
    public IActionResult Index(BackupFilterViewModel filters, int page = 1, int pageSize = 20, string sort = null, string order = null)
    {
        page = Math.Max(1, page);
        
        sort ??= "date";
        order ??= "desc";
        
        var query = DbContext.Backups
            .Include(b => b.Job)
            .Include(b => b.Job.Server).AsQueryable();

        ViewBag.TotalItems = query.Count();
        ViewBag.TotalPages = (int) Math.Ceiling((double) ViewBag.TotalItems / pageSize);

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
                case "ServerId":
                    query = query.Where(b => b.Job.ServerId == (int) value);
                    break;
                case "JobId":
                    query = query.Where(b => b.JobId == (int) value);
                    break;
                case "FileName": 
                    query = query.Where(b => b.Path.Contains(value.ToString()));
                    break;
                case "FileSize":
                    query = query.ToList().Where(b => b.GetFileSizeString().Contains(value.ToString() ?? string.Empty)).AsQueryable();
                    break;
                case "Date":
                    query = query.Where(b => b.BackupDate.Date == (value as DateTime?));
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var descending = order == "desc";

            Func<Backup, dynamic> action = sort switch
            {
                "serverId" => b => b.Job.Server.Name,
                "jobId" => b => b.Job.Name,
                "date" => b => b.BackupDate,
                "fileName" => b => b.FileName,
                _ => null
            };

            if (action == null && sort == "fileSize")
            {
                query = query.ToList().AsQueryable();
                action = b => b.GetFileSize();
            }
            
            if (action != null)
            {
                // ReSharper disable twice PossibleUnintendedQueryableAsEnumerable
                query = (descending ? query.OrderByDescending(action) : query.OrderBy(action)).AsQueryable();
            }
        }

        var backups = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        ViewBag.Filters = filters;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Sort = sort;
        ViewBag.Order = order;

        ViewBag.Servers = DbContext.Servers
            .Select(s => new {s.Name, s.Id})
            .ToList()
            .Select(s => new SelectListItem(s.Name, s.Id.ToString()));
        ViewBag.Jobs = DbContext.BackupJobs
            .Select(j => new{j.Name, j.Id})
            .ToList()
            .Select(j => new SelectListItem(j.Name, j.Id.ToString()));

        return View(backups);
    }
    
    [HttpGet("download/{id:int}")]
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
    
    [HttpGet("delete/{id:int}")]
    [Authorize(Policy = nameof(Policies.EditorRolePolicy))]
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

    [HttpGet("restore/{id:int}")]
    [Authorize(Policy = nameof(Policies.RestorerRolePolicy))]
    public async Task<IActionResult> Restore(int id)
    {
        var backup = await DbContext.Backups
            .Include(b => b.Job)
            .Include(b => b.Job.Server)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (backup is null)
            return NotFound();
        
        if(TempData.TryGetValue("Success", out var successValue))
            ViewBag.Success = successValue;
        
        if(TempData.TryGetValue("Error", out var errorValue))
            ViewBag.Error = errorValue;
        
        return View(backup);
    }
    
    [HttpPost("restore/{id:int}")]
    [Authorize(Policy = nameof(Policies.RestorerRolePolicy))]
    public async Task<IActionResult> RestorePost(int id)
    {
        var backup = await DbContext.Backups
            .Include(b => b.Job)
            .Include(b => b.Job.Server)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (backup is null)
            return NotFound();

        if (backup.Job?.Server is null)
            return BadRequest();

        var restoreService = backup.Job.Server.Type.GetService(Configuration).ForServer(backup.Job.Server);

        try
        {
            await restoreService.RestoreDatabase(backup);
            
            TempData["Success"] = "Database restored successfully";
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            
            TempData["Error"] = e.Message;
        }

        return RedirectToAction("Restore", new { id });
    }
}