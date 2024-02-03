using System.Data;
using System.Globalization;
using Agent.Models;
using Core;
using Core.Models;

namespace Agent.Services;

public class BackupService
{
    private IConfiguration Configuration { get; }
    private HttpClient Client { get; }
    
    public BackupService(IConfiguration configuration, HttpClient client)
    {
        Configuration = configuration;
        Client = client;
    }
    
    public async Task<Backup> Backup(Server server, string database)
    {
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

            return null;
        }
        
        var service = Parameters.Type?.GetService(Configuration)?.ForServer(server);

        return await service!.BackupDatabase(database);
    }

    public async Task Restore(Server server, string databaseNames)
    {
        throw new NotImplementedException();
    }

    public async Task SendToManager(string name, Backup backup = null, Exception exception = null)
    {
        if(backup == null && exception == null)
            throw new Exception("backup or exception must be set");
        
        if(backup != null && exception != null)
            throw new Exception("backup and exception cannot be set at the same time");
        
        if(backup != null)
        {
            var file = new FileInfo(backup.Path);
            
            // send file to manager
            var content = new MultipartFormDataContent
            {
                {new StringContent(name), "name"},
                {new StringContent(file.Name), "fileName"},
                {new StringContent(file.Length.ToString()), "fileSize"},
                {new StringContent(file.Extension), "fileExtension"},
                {new StringContent(file.LastWriteTime.ToString(CultureInfo.InvariantCulture)), "fileLastWriteTime"},
                {new StreamContent(file.OpenRead()), "file", file.Name}
            };
            
            var responseMessage = await Client.PostAsync($"{Configuration["Manager:Url"]}/api/agent/backup-result", content);
            
            if (!responseMessage.IsSuccessStatusCode)
            {
                await Console.Error.WriteLineAsync($"Could not send backup to manager: {responseMessage.StatusCode}: {responseMessage.ReasonPhrase}");
                await Console.Error.WriteLineAsync(await responseMessage.Content.ReadAsStringAsync());
            }

            file.Delete();
        }
        else
        {
            var content = new MultipartFormDataContent
            {
                {new StringContent(name), "name"},
                {new StringContent(exception!.Message), "exceptionMessage"},
                {new StringContent(exception!.StackTrace ?? ""), "exceptionStackTrace"}
            };

            await Client.PostAsync($"{Configuration["Manager:Url"]}/api/agent/backup-result", content);
        }
    }
}