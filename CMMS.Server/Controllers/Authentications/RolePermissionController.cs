using CMMS.Server.Services.PermissionService;
using CMMS.Shared.Dtos.AuthModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMMS.Server.Controllers.Authentications
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class RolePermissionController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public RolePermissionController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet("roles")]
        public async Task<ActionResult<List<RoleDto>>> GetRoles()
        {
            var roles = await _permissionService.GetAllRolesAsync();
            return Ok(roles);
        }

        [HttpGet("permissions/{roleId}")]
        public async Task<ActionResult<List<PermissionPageDto>>> GetPermissions(int roleId)
        {
            var pages = await _permissionService.GetAllPermissionPagesWithPermissionsAsync(roleId);
            return Ok(pages);
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdateRolePermissionsRequest request)
        {
            await _permissionService.UpdateRolePermissionsAsync(request);
            return Ok(new { message = "Permissions updated successfully" });
        }
    }
}
