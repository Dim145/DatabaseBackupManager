using System.Data;
using System.Diagnostics;
using Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Core.Services;

public class SqlServerBackupService: DatabaseBackup
{
    public override async Task<Backup> BackupDatabase(string databaseName, CancellationToken cancellationToken = default)
    {
        if (Server is null)
            return null;
        
        var path = GetPathForBackup(databaseName, Constants.SqlServerBackupFileExtension);

        var cmd = $"sqlcmd -S {Server.Host},{Server.Port} -U {Server.User} -P {Server.Password} -Q \"BACKUP DATABASE [{databaseName}] TO DISK = N'/usr/backup_{databaseName}' WITH NOFORMAT, INIT, NAME = '{databaseName}-full', SKIP, NOREWIND, NOUNLOAD, STATS = 10\"";
        
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "sqlcmd",
            Arguments = $"-S {Server.Host},{Server.Port} -U {Server.User} -P {Server.Password} -Q \"BACKUP DATABASE [{databaseName}] TO DISK = N'/usr/backup_{databaseName}' WITH NOFORMAT, INIT, NAME = '{databaseName}-full', SKIP, NOREWIND, NOUNLOAD, STATS = 10\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        
        await process.WaitForExitAsync(cancellationToken);
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            
            throw new Exception($"sqlcmd failed with exit code {process.ExitCode}: {error}");
        }
        
        Console.WriteLine($"CMD |{cmd}| RES IS {await process.StandardOutput.ReadToEndAsync(cancellationToken)}");
        
        var dbConnection = Server.GetConnection();

        await dbConnection.OpenAsync(cancellationToken);
        
        var command = dbConnection.CreateCommand();
        
        // create temp table to store backup file
        command.CommandText = $"CREATE TABLE #temp_{databaseName} (BackupFile VARBINARY(MAX))";
        await command.ExecuteNonQueryAsync(cancellationToken);

        try
        {
            // read backup file into temp table
            command.CommandText = $"INSERT INTO #temp_{databaseName} (BackupFile) SELECT * FROM OPENROWSET(BULK N'/usr/backup_{databaseName}', SINGLE_BLOB) AS BackupFile";
            await command.ExecuteNonQueryAsync(cancellationToken);
        
            // download backup file from temp table
            command.CommandText = $"SELECT BackupFile FROM #temp_{databaseName}";
            var dataAdapter = new SqlDataAdapter(command as SqlCommand);
        
            var dataSet = new DataSet();
            dataAdapter.Fill(dataSet);

            var backupFile = dataSet.Tables[0].Rows[0];
            var backupFromServer = (byte[]) backupFile["BackupFile"];

            var size = backupFromServer.GetUpperBound(0) + 1;
        
            var fileStream = new FileStream(path, FileMode.Create);
            await fileStream.WriteAsync(backupFromServer, 0, size, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);
            fileStream.Close();
        }
        finally
        {
            // drop temp table
            command.CommandText = $"DROP TABLE #temp_{databaseName}";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return new Backup
        {
            BackupDate = DateTime.UtcNow,
            Path = path,
        };
    }

    public override Task<bool> RestoreDatabase(Backup backup, CancellationToken cancellationToken = default)
    {
        throw new Exception("restoring database is not supported for sql server"); // todo: search for solution ?
    }
}