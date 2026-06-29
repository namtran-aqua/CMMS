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

            // Must belong to the same factory
            if (user.FACID != equipmentFacId)
            {
                return false;
            }

            // Manager role (RoleID = 1) can operate on all equipment in their factory
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
    }
}
