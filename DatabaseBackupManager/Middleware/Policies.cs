using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

using static DatabaseBackupManager.Data.Seeds;

namespace DatabaseBackupManager.Middleware;

internal static class Policies
{
    //Reader = can  view && download
    //Editor = can  view && download && editcreate backups
    //Restorer = can  view && download && editcreate backups && restore backups
    //Admin = can  view && download && editcreate backups && restore backups && manage users

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
        policy.RequireRole(DataSettings.DefaultEditorRole, DataSettings.DefaultRestorerRole, DataSettings.DefaultAdminRole);
    }
    
    internal static void RestorerRolePolicy(AuthorizationPolicyBuilder policy)
    {
        policy.RequireRole(DataSettings.DefaultRestorerRole, DataSettings.DefaultAdminRole);
    }
    
    internal static bool IsInProjectRole(this ClaimsPrincipal user, string role)
    {
        if (user.IsInRole(DataSettings.DefaultAdminRole))
            return true;

        if (role == DataSettings.DefaultRestorerRole)
        {
            return user.IsInRole(DataSettings.DefaultRestorerRole) || 
                   user.IsInRole(DataSettings.DefaultAdminRole);
        }
        
        if (role == DataSettings.DefaultEditorRole)
        {
            return user.IsInRole(DataSettings.DefaultEditorRole) || 
                   user.IsInRole(DataSettings.DefaultRestorerRole) || 
                   user.IsInRole(DataSettings.DefaultAdminRole);
        }
        
        if (role == DataSettings.DefaultReaderRole)
        {
            return user.IsInRole(DataSettings.DefaultReaderRole) || 
                   user.IsInRole(DataSettings.DefaultEditorRole) || 
                   user.IsInRole(DataSettings.DefaultRestorerRole) || 
                   user.IsInRole(DataSettings.DefaultAdminRole);
        }
        
        return false;
    }
}