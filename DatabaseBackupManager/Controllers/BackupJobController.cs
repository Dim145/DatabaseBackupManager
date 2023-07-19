using Core.Models;
using DatabaseBackupManager.Data;
using DatabaseBackupManager.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Controllers;

[Route("backup-jobs")]
[Authorize(Policy = nameof(Policies.ReaderRolePolicy))]
public class BackupJobController: Controller
{
    private ApplicationDbContext DbContext { get; }
    
    public BackupJobController(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<IActionResult> Index(string sn)
    {
        var query = DbContext.BackupJobs.Include(b => b.Server).AsQueryable();

        if (!string.IsNullOrWhiteSpace(sn))
        {
            query = query.Where(s => s.Name.Contains(sn));
        }
        
        var backupJobs = await query.ToListAsync();
        
        ViewBag.SearchName = sn;
        
        return View(backupJobs);
    }

    [HttpGet("new")]
    public IActionResult Create()
    {
        ViewBag.Servers = DbContext.Servers.ToList();
        
        return View(new BackupJob());
    }
    
    [HttpPost("new")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = nameof(Policies.EditorRolePolicy))]
    public async Task<IActionResult> Create(BackupJob backupJob)
    {
        ViewBag.Servers = DbContext.Servers.ToList();
        
        if (backupJob.ServerId <= 0)
        {
            return View(backupJob);
        }
        
        var server = await DbContext.Servers.FirstOrDefaultAsync(s => s.Id == backupJob.ServerId);
        
        ViewBag.Databases = await server.ListDatabases();

        if (string.IsNullOrWhiteSpace(backupJob.DatabaseNames) || string.IsNullOrWhiteSpace(backupJob.Cron) || string.IsNullOrWhiteSpace(backupJob.Name))
        {
            if(backupJob.Retention == default)
                backupJob.Retention = TimeSpan.FromDays(7);
            
            return View(backupJob);
        }
        
        backupJob.Enabled = true;

        DbContext.BackupJobs.Add(backupJob);
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet("edit/{id:int}")]
    [Authorize(Policy = nameof(Policies.EditorRolePolicy))]
    public async Task<IActionResult> Edit(int id)
    {
        var backupJob = await DbContext.BackupJobs.FirstOrDefaultAsync(b => b.Id == id);
        
        if (backupJob == null)
            return NotFound();
        
        return View(backupJob);
    }
    
    [HttpPost("edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = nameof(Policies.EditorRolePolicy))]
    public async Task<IActionResult> Edit(int id, BackupJob backupJob)
    {
        var existingBackupJob = await DbContext.BackupJobs.FirstOrDefaultAsync(b => b.Id == id);
        
        if (existingBackupJob == null)
            return NotFound();
        
        existingBackupJob.Cron = backupJob.Cron;
        existingBackupJob.Enabled = backupJob.Enabled;
        existingBackupJob.Retention = backupJob.Retention;
        
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("change-status/{id:int}")]
    [Authorize(Policy = nameof(Policies.EditorRolePolicy))]
    public IActionResult ChangeStatus(int id)
    {
        var backupJob = DbContext.BackupJobs.FirstOrDefault(b => b.Id == id);
        
        if (backupJob == null)
            return NotFound();
        
        backupJob.Enabled = !backupJob.Enabled;
        
        DbContext.SaveChanges();
        
        return Redirect(Request.Headers["Referer"].ToString());
    }
    
    [HttpGet("delete/{id}")]
    [Authorize(Policy = nameof(Policies.EditorRolePolicy))]
    public async Task<IActionResult> Delete(int id)
    {
        var backupJob = await DbContext.BackupJobs.FirstOrDefaultAsync(b => b.Id == id);
        
        if (backupJob == null)
            return NotFound();
        
        return View(backupJob);
    }
    
    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = nameof(Policies.EditorRolePolicy))]
    public async Task<IActionResult> DeletePost(int id)
    {
        var existingBackupJob = await DbContext.BackupJobs.FirstOrDefaultAsync(b => b.Id == id);
        
        if (existingBackupJob == null)
            return NotFound();
        
        DbContext.BackupJobs.Remove(existingBackupJob);
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
}