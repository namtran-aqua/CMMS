using System;
using System.Collections.Generic;
using CMMS.Shared.Dtos.User;

namespace CMMS.Shared.Authorization
{
    public static class AuthorizationHelper
    {
        public static bool CanEditOrMaintain(UserDto? user, int? equipmentFacId, string? equipmentPicId)
        {
            if (user == null) return false;

            // Admin (RoleID = 3) and IT (RoleID = 4) bypass all factory and PIC checks
            if (user.RoleID == 3 || user.RoleID == 4 || (user.Roles != null && (user.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) || user.Roles.Contains("IT", StringComparer.OrdinalIgnoreCase))))
            {
                return true;
            }

            // Other roles must belong to the same factory
            if (user.FACID != equipmentFacId)
            {
                return false;
            }

            // Manager (RoleID = 1) can operate on all equipment in their factory
            if (user.RoleID == 1 || (user.Roles != null && user.Roles.Contains("Manager", StringComparer.OrdinalIgnoreCase)))
            {
                return true;
            }

            // User/Staff role (RoleID = 2) can only operate on equipment where they are PIC
            if (user.RoleID == 2 || (user.Roles != null && (user.Roles.Contains("User", StringComparer.OrdinalIgnoreCase) || user.Roles.Contains("Staff", StringComparer.OrdinalIgnoreCase))))
            {
                return !string.IsNullOrEmpty(equipmentPicId) && string.Equals(equipmentPicId, user.WorkDayId, StringComparison.OrdinalIgnoreCase);
            }

            // Default safety check: treat as standard User (PIC check)
            return !string.IsNullOrEmpty(equipmentPicId) && string.Equals(equipmentPicId, user.WorkDayId, StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanManageSparePart(UserDto? user, int? partFacId)
        {
            if (user == null) return false;

            // Admin and IT bypass factory check
            if (user.RoleID == 3 || user.RoleID == 4 || (user.Roles != null && (user.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) || user.Roles.Contains("IT", StringComparer.OrdinalIgnoreCase))))
            {
                return true;
            }

            // Must belong to the same factory — that's the only check needed for other roles
            return user.FACID == partFacId;
        }

        public static int? GetAllowedFactoryId(UserDto? user, int? requestedFactoryId)
        {
            if (user == null) return requestedFactoryId;

            // Admin and IT can query any factory
            if (user.RoleID == 3 || user.RoleID == 4 || (user.Roles != null && (user.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) || user.Roles.Contains("IT", StringComparer.OrdinalIgnoreCase))))
            {
                return requestedFactoryId;
            }

            // Manager and User are restricted to their own factory
            return user.FACID;
        }
    }
}
