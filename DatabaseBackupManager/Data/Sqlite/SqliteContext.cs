using DatabaseBackupManager.Services.StorageService;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Data.Sqlite;

public class SqliteContext(
    DbContextOptions<SqliteContext> options,
    IConfiguration conf,
    IStorageService storageService)
    : BaseContext(options, conf, storageService);