using CMMS.Server.Services.DepartmentService;
using Microsoft.Data.SqlClient;
using CMMS.Shared.Dtos.Equipment;

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

            var sql = "SELECT * FROM dbo.vw_FactoryDepartment";

            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand(sql, con);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new DepartmentDto
                {
                    DeptID = (int)reader["DeptID"],
                    DeptName = reader["DeptName"].ToString(),
                    DeptFullName = reader["DeptFullName"].ToString(),
                    DeptCode = reader["DeptCode"].ToString()
                });
            }

            return list;
        }

    }
}
