using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMMS.Server.Services.PermissionService
{
    public interface IPermissionService
    {
        Task<HashSet<string>> GetRolePermissionsAsync(int roleId);
        Task ClearRolePermissionsCacheAsync(int roleId);
        
        Task<List<CMMS.Shared.Dtos.AuthModels.RoleDto>> GetAllRolesAsync();
        Task<List<CMMS.Shared.Dtos.AuthModels.PermissionPageDto>> GetAllPermissionPagesWithPermissionsAsync(int roleId);
        Task UpdateRolePermissionsAsync(CMMS.Shared.Dtos.AuthModels.UpdateRolePermissionsRequest request);
    }
}
