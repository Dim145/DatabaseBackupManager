using DatabaseBackupManager.Services.StorageService;
using Microsoft.EntityFrameworkCore;

namespace DatabaseBackupManager.Data.Postgres;

public class PostgresContext(DbContextOptions<PostgresContext> options, IConfiguration conf, IStorageService storageService)
    : BaseContext(options, conf, storageService);