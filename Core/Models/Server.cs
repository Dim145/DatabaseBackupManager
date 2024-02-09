using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using MySqlConnectionStringBuilder = MySql.Data.MySqlClient.MySqlConnectionStringBuilder;

namespace Core.Models;

public class Server: BaseModel, IDatabase
{
    [Required] 
    public DatabaseTypes Type { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    [Required]
    public string Host { get; set; }
    
    [Required]
    public string User { get; set; }
    
    [DataType(DataType.Password)]
    public string Password { get; set; }
    
    [Required]
    [Range(1, 65535)]
    public int Port { get; set; }

    private string _connectionString;
    
    public List<BackupJob> BackupJobs { get; set; } = new();

    [NotMapped]
    [ValidateNever]
    public string ConnectionString => _connectionString ??= Type switch
    {
        DatabaseTypes.Postgres => new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Username = User,
            Port = Port,
            IncludeErrorDetail = true,
            Password = Password
        }.ToString(),
        DatabaseTypes.MySql => new MySqlConnectionStringBuilder
        {
            Port = (uint)Port,
            Password = Password,
            Server = Host,
            UserID = User,
            AllowUserVariables = true
        }.ToString(),
        DatabaseTypes.SqlServer => new SqlConnectionStringBuilder
        {
            Password = Password,
            DataSource = $"{Host},{Port}",
            UserID = User,
            TrustServerCertificate = true
        }.ToString(),
        DatabaseTypes.Sqlite => new SqliteConnectionStringBuilder
        {
            DataSource = Host,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Password = Password
        }.ToString(),
        
        _ => throw new ArgumentOutOfRangeException(nameof(Type), "Database type not supported")
    };

    public async Task<string[]> ListDatabases()
    {
        if (Type == DatabaseTypes.Sqlite)
        {
            return new[] { "all tables" };
        }
        
        var connection = GetConnection();
        
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        
        command.CommandText = Type switch
        {
            DatabaseTypes.Postgres => "SELECT datname FROM pg_database WHERE datistemplate = false;",
            DatabaseTypes.MySql => "SHOW DATABASES;",
            DatabaseTypes.SqlServer => "SELECT name FROM master.dbo.sysdatabases;",
            _ => throw new ArgumentOutOfRangeException(nameof(Type), "Database type not supported")
        };
        
        var reader = await command.ExecuteReaderAsync();
        
        var databases = new List<string>();
        
        while (await reader.ReadAsync())
        {
            databases.Add(reader.GetString(0));
        }
        
        await connection.CloseAsync();
        
        return databases.ToArray();
    }

    public DbConnection GetConnection() => Type switch
    {
        DatabaseTypes.Postgres => new NpgsqlConnection(ConnectionString),
        DatabaseTypes.MySql => new MySqlConnection(ConnectionString),
        DatabaseTypes.SqlServer => new SqlConnection(ConnectionString),
        DatabaseTypes.Sqlite => new SqliteConnection(ConnectionString),
        _ => throw new Exception($"Server type {Type} is not supported")
    };
}