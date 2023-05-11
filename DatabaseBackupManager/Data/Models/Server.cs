using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DatabaseBackupManager.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Npgsql;

namespace DatabaseBackupManager.Data.Models;

public class Server: BaseModel
{
    public DatabaseTypes Type { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    public string Host { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public int Port { get; set; }

    private string _connectionString;

    [NotMapped]
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
            UserID = User
        }.ToString(),
        DatabaseTypes.Sqlite => new SqliteConnectionStringBuilder
        {
            DataSource = Host,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString(),
        
        _ => throw new ArgumentOutOfRangeException(nameof(Type), "Database type not supported")
    };
}