using System.Text;
using Agent.Models;
using Core.Models;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Agent.Services;

public class PingPongService: IHostedService
{
    private bool Stopped { get; set; } = false;
    
    private HttpClient Client { get; }
    
    private BackupService BackupService { get; }
    
    public PingPongService(IServiceProvider serviceProvider, HttpClient client)
    {
        Client = client;
        
        BackupService = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<BackupService>();
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
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
        
        while (!Stopped)
        {
            try
            {
                var databases = await server.ListDatabases();
                
                var response = await Client.PostAsync($"{Parameters.ManagerUrl}/api/agent/ping", new StringContent(JsonSerializer.Serialize(new
                {
                    Parameters.Token,
                    Databases = databases
                }), Encoding.UTF8, "application/json"), cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);

                    var jobs = JsonConvert.DeserializeAnonymousType(content, new
                    {
                        Jobs = new[]
                        {
                            new
                            {
                                Name = "",
                                DatabaseNames = "",
                                Type = false
                            }
                        }
                    })?.Jobs;

                    if (jobs != null)
                    {
                        foreach (var job in jobs)
                        {
                            Backup backup = null;
                            Exception ex = null;
                            
                            try
                            {
                                if (job.Type)
                                {
                                    backup = await BackupService.Backup(server, job.DatabaseNames);
                                }
                                else
                                {
                                    await BackupService.Restore(server, job.DatabaseNames);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine(e);
                                ex = e;
                            }
                            finally
                            {
                                await BackupService.SendToManager(job.Name, backup, ex);
                            }
                        }
                    }
                    else
                    {
                        await Console.Error.WriteLineAsync($"Could not deserialize agent: {content}");
                    }
                }
                else
                {
                    await Console.Error.WriteLineAsync($"Could not ping server: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Stopped = true;
        
        return Task.CompletedTask;
    }
}