using CMMS.Data.Connection;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMMS.Server.Services.PermissionService
{
    public class PermissionService : IPermissionService
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IMemoryCache _cache;

        public PermissionService(ISqlConnectionFactory connectionFactory, IMemoryCache cache)
        {
            _connectionFactory = connectionFactory;
            _cache = cache;
        }

        public async Task<HashSet<string>> GetRolePermissionsAsync(int roleId)
        {
            var cacheKey = $"ROLE_PERMISSIONS_{roleId}";

            if (_cache.TryGetValue(cacheKey, out HashSet<string> cachedPermissions))
            {
                return cachedPermissions;
            }

            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT 
                    pp.ModuleCode + '.' + pp.PageCode + '.' + p.ActionCode AS PermissionCode
                FROM Tbl_RolePermissions rp
                JOIN Tbl_Permissions p ON rp.PermissionID = p.PermissionID
                JOIN Tbl_PermissionPages pp ON p.PermissionPageID = pp.PermissionPageID
                WHERE rp.RoleID = @RoleID AND p.IsActive = 1 AND pp.IsActive = 1
            ";

            var permissionsList = await connection.QueryAsync<string>(sql, new { RoleID = roleId });
            var permissionsSet = new HashSet<string>(permissionsList);

            _cache.Set(cacheKey, permissionsSet, System.TimeSpan.FromMinutes(30));

            return permissionsSet;
        }

        public Task ClearRolePermissionsCacheAsync(int roleId)
        {
            var cacheKey = $"ROLE_PERMISSIONS_{roleId}";
            _cache.Remove(cacheKey);
            return Task.CompletedTask;
        }

        public async Task<List<CMMS.Shared.Dtos.AuthModels.RoleDto>> GetAllRolesAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT RoleID, RoleCode, RoleName, Description, IsActive FROM Tbl_SystemRoles WHERE IsActive = 1 ORDER BY RoleID";
            return (await connection.QueryAsync<CMMS.Shared.Dtos.AuthModels.RoleDto>(sql)).ToList();
        }

        public async Task<List<CMMS.Shared.Dtos.AuthModels.PermissionPageDto>> GetAllPermissionPagesWithPermissionsAsync(int roleId)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var pagesSql = "SELECT PermissionPageID, ModuleCode, PageCode, PageName, DisplayOrder FROM Tbl_PermissionPages WHERE IsActive = 1 ORDER BY DisplayOrder";
            var pages = (await connection.QueryAsync<CMMS.Shared.Dtos.AuthModels.PermissionPageDto>(pagesSql)).ToList();

            var permissionsSql = @"
                SELECT 
                    p.PermissionID, 
                    p.PermissionPageID, 
                    p.ActionCode, 
                    p.ActionName, 
                    p.DisplayOrder,
                    pp.ModuleCode + '.' + pp.PageCode + '.' + p.ActionCode AS FullPermissionCode,
                    CAST(CASE WHEN rp.PermissionID IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsGranted
                FROM Tbl_Permissions p
                JOIN Tbl_PermissionPages pp ON p.PermissionPageID = pp.PermissionPageID
                LEFT JOIN Tbl_RolePermissions rp ON p.PermissionID = rp.PermissionID AND rp.RoleID = @RoleID
                WHERE p.IsActive = 1
                ORDER BY p.DisplayOrder
            ";
            
            var allPermissions = await connection.QueryAsync<dynamic>(permissionsSql, new { RoleID = roleId });

            foreach (var page in pages)
            {
                page.Permissions = allPermissions
                    .Where(p => p.PermissionPageID == page.PermissionPageID)
                    .Select(p => new CMMS.Shared.Dtos.AuthModels.PermissionDto
                    {
                        PermissionID = p.PermissionID,
                        ActionCode = p.ActionCode,
                        ActionName = p.ActionName,
                        DisplayOrder = p.DisplayOrder,
                        FullPermissionCode = p.FullPermissionCode,
                        IsGranted = p.IsGranted
                    }).ToList();
            }

            return pages;
        }

        public async Task UpdateRolePermissionsAsync(CMMS.Shared.Dtos.AuthModels.UpdateRolePermissionsRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            // Start transaction manually since we are using Dapper
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Delete existing permissions for the role
                await connection.ExecuteAsync("DELETE FROM Tbl_RolePermissions WHERE RoleID = @RoleID", new { request.RoleID }, transaction);

                // Insert new permissions
                if (request.PermissionIDs != null && request.PermissionIDs.Any())
                {
                    var insertSql = "INSERT INTO Tbl_RolePermissions (RoleID, PermissionID) VALUES (@RoleID, @PermissionID)";
                    var parameters = request.PermissionIDs.Select(pid => new { RoleID = request.RoleID, PermissionID = pid });
                    await connection.ExecuteAsync(insertSql, parameters, transaction);
                }

                transaction.Commit();
                
                // Invalidate cache
                await ClearRolePermissionsCacheAsync(request.RoleID);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
