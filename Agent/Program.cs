using System.Text;
using Agent.Models;
using Agent.Services;
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

builder.Services.AddHostedService<PingPongService>();

// get .net server url
var serverUrl = Environment.GetEnvironmentVariable("AGENT_URL") ?? "http://localhost:5000";

builder.Services.AddHttpClient<BackupService>(c =>
{
    c.DefaultRequestHeaders.Add("Agent-Url", serverUrl);
});

builder.Services.AddHttpClient<PingPongService>(c =>
{
    c.DefaultRequestHeaders.Add("Agent-Url", serverUrl);
});

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

await app.RunAsync(cancellationTokenSource.Token);