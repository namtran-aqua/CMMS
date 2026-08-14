using System.Collections.Generic;

namespace CMMS.Shared.Dtos.AuthModels
{
    public class RoleDto
    {
        public int RoleID { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class PermissionPageDto
    {
        public int PermissionPageID { get; set; }
        public string ModuleCode { get; set; } = string.Empty;
        public string PageCode { get; set; } = string.Empty;
        public string PageName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new();
    }

    public class PermissionDto
    {
        public int PermissionID { get; set; }
        public string ActionCode { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public string FullPermissionCode { get; set; } = string.Empty; // e.g. SPAREPART.INBOUND.VIEW
        public bool IsGranted { get; set; }
    }

    public class UpdateRolePermissionsRequest
    {
        public int RoleID { get; set; }
        public List<int> PermissionIDs { get; set; } = new();
    }
}
