using System.Collections.Generic;

namespace Fintech.Core.Domain;

public static class RolePermissionStore
{
    private static readonly Dictionary<UserRole, HashSet<Permission>> _rolePermissions = new()
    {
        [UserRole.super_admin] = new HashSet<Permission>(Enum.GetValues<Permission>()),
        
        [UserRole.branch_manager] = new HashSet<Permission>
        {
            Permission.VIEW_USERS,
            Permission.VIEW_LOAN,
            Permission.CREATE_LOAN,
            Permission.DISBURSE_LOAN,
            Permission.CLOSE_LOAN,
            Permission.UPDATE_PAYMENT,
            Permission.VIEW_COLLECTIONS,
            Permission.VIEW_REPORTS,
            Permission.VIEW_TRANSACTIONS
        },

        [UserRole.loan_officer] = new HashSet<Permission>
        {
            Permission.VIEW_LOAN,
            Permission.CREATE_LOAN,
            Permission.VIEW_COLLECTIONS
        },

        [UserRole.accountant] = new HashSet<Permission>
        {
            Permission.VIEW_LOAN,
            Permission.VIEW_REPORTS,
            Permission.VIEW_TRANSACTIONS,
            Permission.MANAGE_ACCOUNTS
        },

        [UserRole.collection_officer] = new HashSet<Permission>
        {
            Permission.VIEW_USERS,
            Permission.VIEW_LOAN,
            Permission.UPDATE_PAYMENT,
            Permission.VIEW_COLLECTIONS
        },

        [UserRole.recovery_specialist] = new HashSet<Permission>
        {
            Permission.VIEW_LOAN,
            Permission.RECOVERY_ACTIONS,
            Permission.VIEW_COLLECTIONS
        },

        [UserRole.agent] = new HashSet<Permission>
        {
            Permission.VIEW_LOAN,
            Permission.VIEW_COLLECTIONS
        },

        [UserRole.customer] = new HashSet<Permission>
        {
            Permission.VIEW_LOAN
        },

        [UserRole.partner] = new HashSet<Permission>
        {
            Permission.VIEW_LOAN,
            Permission.VIEW_REPORTS
        },

        [UserRole.agent] = new HashSet<Permission>
        {
            Permission.VIEW_USERS,
            Permission.VIEW_LOAN,
            Permission.UPDATE_PAYMENT,
            Permission.VIEW_COLLECTIONS
        }
    };

    public static bool HasPermission(UserRole role, Permission permission)
    {
        if (_rolePermissions.TryGetValue(role, out var permissions))
        {
            return permissions.Contains(permission);
        }
        return false;
    }
}
