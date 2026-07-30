using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;
using Microsoft.Data.SqlClient;
using Dapper;

namespace CMMS.Server.Services.LocationService
{
    public class LocationService : ILocationService
    {
        private readonly IConfiguration _config;
        public LocationService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<List<LocationDto>> GetLocationsAsync()
        {
            var sql = "SELECT * FROM dbo.Tbl_FactoryLocation";
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var list = await con.QueryAsync<LocationDto>(sql);
            return list.ToList();
        }

        public async Task<LocationDto> GetLocationAsync(int id)
        {
            var sql = "SELECT * FROM dbo.Tbl_FactoryLocation WHERE LocID = @Id";
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            return await con.QueryFirstOrDefaultAsync<LocationDto>(sql, new { Id = id });
        }

        public async Task<ApiResponse> CreateLocationAsync(LocationDto location)
        {
            try
            {
                var sql = @"INSERT INTO dbo.Tbl_FactoryLocation 
                            (DeptID, FACID, LocName, LocCode, LocManager)
                            VALUES (@DeptID, @FACID, @LocName, @LocCode, @LocManager)";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, location);
                return new ApiResponse { Success = true, Message = "Location created successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse> UpdateLocationAsync(LocationDto location)
        {
            try
            {
                var sql = @"UPDATE dbo.Tbl_FactoryLocation 
                            SET DeptID = @DeptID, FACID = @FACID, LocName = @LocName, 
                                LocCode = @LocCode, LocManager = @LocManager
                            WHERE LocID = @LocID";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, location);
                return new ApiResponse { Success = true, Message = "Location updated successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse> DeleteLocationAsync(int id)
        {
            try
            {
                var sql = "DELETE FROM dbo.Tbl_FactoryLocation WHERE LocID = @Id";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, new { Id = id });
                return new ApiResponse { Success = true, Message = "Location deleted successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }
    }
}
