using CMMS.Server.Services.VendorService;
using Microsoft.Data.SqlClient;
using static Org.BouncyCastle.Math.EC.ECCurve;
using CMMS.Shared.Dtos.Equipment;

namespace CMMS.Server.Services.VendorService
{
    public class VendorService : IVendorService
    {
            private readonly IConfiguration _config;
            public VendorService(IConfiguration config)
            {
                _config = config;
            }
            public async Task<List<VendorDto>> GetVendorsAsync()
            {
                var list = new List<VendorDto>();
    
                var sql = "SELECT * FROM dbo.Tbl_Vendors";
    
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                using var cmd = new SqlCommand(sql, con);
    
                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
    
                while (await reader.ReadAsync())
                {
                    list.Add(new VendorDto
                    {
                        VendorID = (int)reader["VendorID"],
                        VendorName = reader["VendorName"].ToString()
                    });
                }
    
                return list;
        }
    }
}
