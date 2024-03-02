using System.Data;
using Agent.Models;
using Core;
using Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Agent.Controllers;

[ApiController]
[Route("[controller]")]
public class BackupController: ControllerBase
{
    public class BackupRequest
    {
        public string Databases { get; set; }
        public string Token { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Backup([FromBody] BackupRequest  backupRequest)
    {
        if(backupRequest.Token != Parameters.Token)
            return Unauthorized();
        
        if(string.IsNullOrEmpty(backupRequest.Databases))
            return BadRequest("databases is required");

        var server = new Server
        {
            Type = Parameters.Type!.Value,
            Host = Parameters.DatabaseHost,
            User = Parameters.DatabaseUsername,
            Password = Parameters.DatabasePassword,
            Port = Parameters.DatabasePort,
            Id = 1,
            Name = "Agent"
        };

        var connection = server.GetConnection();

        try
        {
            await connection.OpenAsync();

            if (connection.State != ConnectionState.Open)
                throw new Exception("State is  not open");

            await connection.CloseAsync();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);

            return StatusCode(500, "Could not connect to database");
        }
        
        var service = Parameters.Type?.GetService()?.ForServer(server);

        var backup = await service!.BackupDatabase(backupRequest.Databases);
        
        if (backup is null)
            return StatusCode(500, "Could not backup database");

        if (System.IO.File.Exists(backup.Path))
        {
            return PhysicalFile(backup.Path, "application/octet-stream", backup.FileName);
        }

        return StatusCode(500, "no backup file found");
    }
}