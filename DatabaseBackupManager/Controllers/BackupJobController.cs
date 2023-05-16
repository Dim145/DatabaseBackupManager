using DatabaseBackupManager.Data;
using DatabaseBackupManager.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Controllers;

[Route("backup-jobs")]
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
            return View(backupJob);
        }
        
        backupJob.Enabled = true;

        DbContext.BackupJobs.Add(backupJob);
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var backupJob = await DbContext.BackupJobs.FirstOrDefaultAsync(b => b.Id == id);
        
        if (backupJob == null)
            return NotFound();
        
        return View(backupJob);
    }
    
    [HttpPost("edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BackupJob backupJob)
    {
        if (!ModelState.IsValid)
            return View(backupJob);
        
        var existingBackupJob = await DbContext.BackupJobs.FirstOrDefaultAsync(b => b.Id == id);
        
        if (existingBackupJob == null)
            return NotFound();
        
        existingBackupJob.Name = backupJob.Name;
        existingBackupJob.Cron = backupJob.Cron;
        existingBackupJob.Enabled = backupJob.Enabled;
        existingBackupJob.DatabaseNames = backupJob.DatabaseNames;
        existingBackupJob.BackupFormat = backupJob.BackupFormat;
        existingBackupJob.ServerId = backupJob.ServerId;
        
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet("delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var backupJob = await DbContext.BackupJobs.FirstOrDefaultAsync(b => b.Id == id);
        
        if (backupJob == null)
            return NotFound();
        
        return View(backupJob);
    }
    
    [HttpPost("delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, BackupJob backupJob)
    {
        var existingBackupJob = await DbContext.BackupJobs.FirstOrDefaultAsync(b => b.Id == id);
        
        if (existingBackupJob == null)
            return NotFound();
        
        DbContext.BackupJobs.Remove(existingBackupJob);
        await DbContext.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
}