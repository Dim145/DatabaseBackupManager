using System.Text;
using Agent.Models;
using Core.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Parameters.DatabasePort = int.TryParse(Environment.GetEnvironmentVariable("DATABASE_PORT"),  out var port) ? port : -1;
Parameters.DatabaseHost = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "localhost";
Parameters.DatabaseUsername = Environment.GetEnvironmentVariable("DATABASE_USERNAME") ?? "root";
Parameters.DatabasePassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "root";

Parameters.ManagerUrl = Environment.GetEnvironmentVariable("MANAGER_URL") ?? "http://localhost:5000";
Parameters.Token = Environment.GetEnvironmentVariable("TOKEN");
Parameters.Type = Enum.TryParse<DatabaseTypes>(Environment.GetEnvironmentVariable("DATABASE_TYPE"), out var type) ? type : null;

if(!Parameters.IsParametersValid())
    throw new Exception("Invalid parameters");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

var cancellationTokenSource = new CancellationTokenSource();

var appTask = app.RunAsync(cancellationTokenSource.Token);

var client = new HttpClient();

// get .net server url
var serverUrl = Environment.GetEnvironmentVariable("AGENT_URL") ?? "http://localhost:5000";
client.DefaultRequestHeaders.Add("Agent-Url", serverUrl);

while (!appTask.IsCompleted)
{
    try
    {
        var response = await client.PostAsync($"{Parameters.ManagerUrl}/api/agents/notify-presence", new StringContent($"{{\"token\": \"{Parameters.Token}\"}}", Encoding.UTF8, "application/json"));

        Console.WriteLine(response.IsSuccessStatusCode ? "Heartbeat sent" : "Heartbeat failed");
    }
    catch (Exception e)
    {
        Console.Error.WriteLine(e);
    }

    await Task.Delay(TimeSpan.FromMinutes(5), cancellationTokenSource.Token);
}

if(appTask.IsFaulted)
    throw appTask.Exception!;