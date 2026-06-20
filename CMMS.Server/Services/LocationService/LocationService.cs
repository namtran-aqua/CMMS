using CMMS.Shared.Dtos.Equipment;
using Microsoft.Data.SqlClient;

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
            var list = new List<LocationDto>();
            var sql = "SELECT * FROM dbo.Tbl_FactoryLocation";
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand(sql, con);
            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new LocationDto
                {
                    LocID = (int)reader["LocID"],
                    LocName = reader["LocName"].ToString(),
                    LocCode = reader["LocCode"].ToString(),
                    LocManager = reader["LocManager"].ToString()
                });
            }
            return list;
        }
    }
}
