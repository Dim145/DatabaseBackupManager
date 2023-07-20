using Agent.Models;
using Core.Models;
using Core;
using Microsoft.AspNetCore.Mvc;

namespace Agent.Controllers;

[ApiController]
[Route("[controller]")]
public class DatabaseController: ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> List([FromBody] string token)
    {
        if(token != Parameters.Token)
            return Unauthorized();
        
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
        
        return Ok(await server.ListDatabases());
    }
}