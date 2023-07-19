using Core.Models;

namespace Agent.Models;

internal static class Parameters
{
    public static string Token { get; set; }
    public static string ManagerUrl { get; set; }
    public static DatabaseTypes? Type { get; set; }
    
    public static string DatabaseHost { get; set; }
    public static string DatabaseUsername { get; set; }
    public static string DatabasePassword { get; set; }
    public static int DatabasePort { get; set; }
    
    public static bool IsParametersValid()
    {
        return !string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(ManagerUrl) && Type != null;
    }
}