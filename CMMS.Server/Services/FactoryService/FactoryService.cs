using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;
using Microsoft.Data.SqlClient;
using Dapper;

namespace CMMS.Server.Services.FactoryService
{
    public class FactoryService : IFactoryService
    {
        private readonly IConfiguration _config;
        public FactoryService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<List<FactoryDto>> GetFactoriesAsync()
        {
            var sql = "SELECT * FROM dbo.Tbl_Factory";
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var list = await con.QueryAsync<FactoryDto>(sql);
            return list.ToList();
        }

        public async Task<FactoryDto> GetFactoryAsync(int id)
        {
            var sql = "SELECT * FROM dbo.Tbl_Factory WHERE FACID = @Id";
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            return await con.QueryFirstOrDefaultAsync<FactoryDto>(sql, new { Id = id });
        }

        public async Task<ApiResponse> CreateFactoryAsync(FactoryDto factory)
        {
            try
            {
                var sql = @"INSERT INTO dbo.Tbl_Factory 
                            (FACCode, FACName, FACFullName)
                            VALUES (@FACCode, @FACName, @FACFullName)";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, factory);
                return new ApiResponse { Success = true, Message = "Factory created successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse> UpdateFactoryAsync(FactoryDto factory)
        {
            try
            {
                var sql = @"UPDATE dbo.Tbl_Factory 
                            SET FACCode = @FACCode, FACName = @FACName, FACFullName = @FACFullName
                            WHERE FACID = @FACID";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, factory);
                return new ApiResponse { Success = true, Message = "Factory updated successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse> DeleteFactoryAsync(int id)
        {
            try
            {
                var sql = "DELETE FROM dbo.Tbl_Factory WHERE FACID = @Id";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, new { Id = id });
                return new ApiResponse { Success = true, Message = "Factory deleted successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }
    }
}
