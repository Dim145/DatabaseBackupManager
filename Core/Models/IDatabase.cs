namespace Core.Models;

public interface IDatabase
{
    DatabaseTypes Type { get; set; }
    string Name { get; set; }
    
    Task<string[]> ListDatabases();
}