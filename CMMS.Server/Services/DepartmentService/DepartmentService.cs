using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;
using Microsoft.Data.SqlClient;
using Dapper;

namespace CMMS.Server.Services.DepartmentService
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IConfiguration _config;
        public DepartmentService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<List<DepartmentDto>> GetDepartmentsAsync()
        {
            var sql = "SELECT * FROM dbo.vw_FactoryDepartment";
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var list = await con.QueryAsync<DepartmentDto>(sql);
            return list.ToList();
        }

        public async Task<List<DepartmentDto>> GetDepartmentsByFactoryAsync(int factoryId)
        {
            var sql = "SELECT * FROM dbo.vw_FactoryDepartment WHERE FACID = @FactoryId";
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var list = await con.QueryAsync<DepartmentDto>(sql, new { FactoryId = factoryId });
            return list.ToList();
        }

        public async Task<DepartmentDto> GetDepartmentAsync(int id)
        {
            var sql = "SELECT * FROM dbo.vw_FactoryDepartment WHERE DeptID = @Id";
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            return await con.QueryFirstOrDefaultAsync<DepartmentDto>(sql, new { Id = id });
        }

        public async Task<ApiResponse> CreateDepartmentAsync(DepartmentDto department)
        {
            try
            {
                var sql = @"INSERT INTO dbo.Tbl_FactoryDepartment 
                            (FACID, DeptCode, DeptName, HODWD)
                            VALUES (@FACID, @DeptCode, @DeptName, @HODWD)";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, department);
                return new ApiResponse { Success = true, Message = "Department created successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse> UpdateDepartmentAsync(DepartmentDto department)
        {
            try
            {
                var sql = @"UPDATE dbo.Tbl_FactoryDepartment 
                            SET FACID = @FACID, DeptCode = @DeptCode, 
                                DeptName = @DeptName, HODWD = @HODWD
                            WHERE DeptID = @DeptID";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, department);
                return new ApiResponse { Success = true, Message = "Department updated successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse> DeleteDepartmentAsync(int id)
        {
            try
            {
                var sql = "DELETE FROM dbo.Tbl_FactoryDepartment WHERE DeptID = @Id";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, new { Id = id });
                return new ApiResponse { Success = true, Message = "Department deleted successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }
    }
}
