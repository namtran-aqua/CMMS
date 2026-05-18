using CMMS.Shared.Equipment;
using CMMS.Server.Services.DepartmentService;
using Microsoft.Data.SqlClient;
using static Org.BouncyCastle.Math.EC.ECCurve;

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
            var list = new List<DepartmentDto>();

            var sql = "SELECT * FROM Tbl_FactoryDepartment";

            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand(sql, con);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new DepartmentDto
                {
                    ID = (int)reader["DeptID"],
                    DeptName = reader["DeptName"].ToString()
                });
            }

            return list;
        }

    }
}
