using Core;
using Core.Models;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Middleware;
using DatabaseBackupManager.Models;
using DatabaseBackupManager.Services.StorageService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Controllers;

[Route("backups")]
[Authorize(Policy = nameof(Policies.ReaderRolePolicy))]
public class BackupController(BaseContext dbContext, IStorageService storageService)
    : Controller
{
    private BaseContext DbContext { get; } = dbContext;
    private IStorageService StorageService { get; } = storageService;


    [HttpGet]
    public IActionResult Index(BackupFilterViewModel filters, int page = 1, int pageSize = 20, string sort = null, string order = null)
    {
        page = Math.Max(1, page);
        
        sort ??= "date";
        order ??= "desc";
        
        var query = DbContext.Backups
            .Include(b => b.Job)
            .AsQueryable();

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
                case "Size":
                    query = query.ToList().Where(b =>
                    {
                        try
                        {
                            return b.Size
                                .ToSizeString()
                                .Contains(value.ToString() ?? string.Empty);
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e);
                            return true;
                        }
                    }).AsQueryable();
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
                "size" => b => b.Size,
                _ => null
            };
            
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
            .FirstOrDefaultAsync(b => b.Id == id);

        if (backup is null)
        {
            return NotFound();
        }

        var fileLink = await StorageService.DownloadLink(backup.Path, Seeds.StorageSettings.S3LinkExpiration);
        
        if($"{fileLink}".StartsWith("http"))
            return Redirect(fileLink);
        
        return File(fileLink, "application/octet-stream", backup.FileName);
    }
    
    [HttpGet("delete/{id:int}")]
    [Authorize(Policy = nameof(Policies.EditorRolePolicy))]
    public async Task<IActionResult> Delete(int id)
    {
        var backup = await DbContext.Backups
            .FirstOrDefaultAsync(b => b.Id == id);

        if (backup is null)
            return NotFound();

        await StorageService.Delete(backup.Path);
        
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
            .FirstOrDefaultAsync(b => b.Id == id);

        if (backup is null)
            return NotFound();
        
        if(TempData.TryGetValue("Success", out var successValue))
            ViewBag.Success = successValue;
        
        if(TempData.TryGetValue("Error", out var errorValue))
            ViewBag.Error = errorValue;
        
        ViewBag.Server = await DbContext.Servers.FirstOrDefaultAsync(s => s.Id == backup.Job.ServerId);
        
        return View(backup);
    }
    
    [HttpPost("restore/{id:int}")]
    [Authorize(Policy = nameof(Policies.RestorerRolePolicy))]
    public async Task<IActionResult> RestorePost(int id)
    {
        var backup = await DbContext.Backups
            .AsNoTracking()
            .Include(b => b.Job)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (backup is null)
            return NotFound();

        if (backup.Job is null)
            return BadRequest();

        switch (backup.Job.ServerType)
        {
            case nameof(Server):
                var server = await DbContext.Servers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == backup.Job.ServerId);
                
                var restoreService = server.Type.GetService().ForServer(server);

                try
                {
                    var tempFileInfo = await StorageService.Get(backup.Path);
                    
                    backup.Path = tempFileInfo.FullName;
                    
                    await restoreService.RestoreDatabase(backup);
                    
                    if(System.IO.File.Exists(tempFileInfo.FullName))
                        System.IO.File.Delete(tempFileInfo.FullName);
            
                    TempData["Success"] = "Database restored successfully";
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
            
                    TempData["Error"] = e.Message;
                }
                
                return RedirectToAction("Restore", new { id });
            
            default:
                return BadRequest($"ServerType {backup.Job.ServerType} is not supported for restoring now");
        }
    }
}