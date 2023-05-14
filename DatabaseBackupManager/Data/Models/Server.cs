using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DatabaseBackupManager.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Npgsql;

namespace DatabaseBackupManager.Data.Models;

public class Server: BaseModel
{
    [Required] 
    public DatabaseTypes Type { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    [Required]
    public string Host { get; set; }
    
    [Required]
    public string User { get; set; }
    
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }
    
    [Required]
    [Range(1, 65535)]
    public int Port { get; set; }

    private string _connectionString;

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