using CMMS.Server.Services.StatusUsingService;
using Microsoft.Data.SqlClient;
using static Org.BouncyCastle.Math.EC.ECCurve;
using CMMS.Shared.Dtos.Equipment;

namespace CMMS.Server.Services.StatusUsingService
{
    public class StatusUsingService : IStatusUsingService
    {
            private readonly IConfiguration _config;
            public StatusUsingService(IConfiguration config)
            {
                _config = config;
            }
            public async Task<List<StatusUsingDto>> GetStatusUsingAsync()
            {
                var list = new List<StatusUsingDto>();
    
                var sql = "SELECT * FROM dbo.Tbl_StatusUsing";
    
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                using var cmd = new SqlCommand(sql, con);
    
                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
    
                while (await reader.ReadAsync())
                {
                    list.Add(new StatusUsingDto
                    {
                        StsUseID = (int)reader["StsUseID"],
                        StsUseName = reader["StsUseName"].ToString()
                    });
                }
    
                return list;
        }
    }
}
