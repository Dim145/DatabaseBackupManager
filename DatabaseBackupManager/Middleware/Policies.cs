using DatabaseBackupManager.Data;
using Microsoft.AspNetCore.Authorization;

using static DatabaseBackupManager.Data.Seeds;

namespace DatabaseBackupManager.Middleware;

internal static class Policies
{
    internal static void AdminRolePolicy(AuthorizationPolicyBuilder policy)
    {
        policy.RequireRole(DataSettings.DefaultAdminRole);
    }
    
    internal static void ReaderRolePolicy(AuthorizationPolicyBuilder policy)
    {
        policy.RequireRole(DataSettings.DefaultReaderRole, 
            DataSettings.DefaultAdminRole, 
            DataSettings.DefaultEditorRole, 
            DataSettings.DefaultRestorerRole
        );
    }
    
    internal static void EditorRolePolicy(AuthorizationPolicyBuilder policy)
    {
        policy.RequireRole(DataSettings.DefaultEditorRole, DataSettings.DefaultAdminRole);
    }
    
    internal static void RestorerRolePolicy(AuthorizationPolicyBuilder policy)
    {
        policy.RequireRole(DataSettings.DefaultRestorerRole, DataSettings.DefaultAdminRole);
    }
}