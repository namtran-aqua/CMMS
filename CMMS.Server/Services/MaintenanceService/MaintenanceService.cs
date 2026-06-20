using CMMS.Client.Pages.Equipment;
using CMMS.Data.Connection;
using CMMS.Shared.Dtos.Maintenance;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace CMMS.Server.Services.MaintenanceService
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IConfiguration _config;
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MaintenanceService(IConfiguration config, ISqlConnectionFactory connectionFactory, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _connectionFactory = connectionFactory;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<List<MaintenanceDto>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM Tbl_MaintenanceRecord";

            var result = await connection.QueryAsync<MaintenanceDto>(sql);
            return result.ToList();
        }
        public async Task<bool> CreatedAsync(MaintenanceDto maintenance, int ID)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userId = user?
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            var workDayId = user?
                .FindFirst(ClaimTypes.SerialNumber)?
                .Value;

            var fullName = user?
                .FindFirst(ClaimTypes.Name)?
                .Value;
            var connStr = _config.GetConnectionString("DefaultConnection");

            const string sqlInsert = @"
                INSERT INTO dbo.Tbl_MaintenanceRecord 
                    (EQID, UpdateBy, UpdateTime, StsMainID, MaintDate, VendorID, MaintPrice, PICID, MaintPIC, MaintDescription, MaintNote, IsEQActive)
                VALUES 
                    (@EQID, @UpdateBy, @UpdateTime, @StsMainID, @MaintDate, @VendorID, @MaintPrice, @PICID, @MaintPIC, @MaintDescription, @MaintNote, @IsEQActive)";

            const string sqlUpdateEquipment = @"
                UPDATE dbo.Tbl_EquipmentInfo
                SET LastMaintenanceDate = @MaintDate,
                    IsActive = @IsActive
                WHERE EQID = @EQID";

            await using var con = new SqlConnection(connStr);
            await con.OpenAsync();

            // 👉 Dùng transaction để đảm bảo không bị lệch dữ liệu
            await using var tran = await con.BeginTransactionAsync();

            try
            {
                await using (var cmd = new SqlCommand(sqlInsert, con, (SqlTransaction)tran))
                {
                    cmd.Parameters.Add("@EQID", SqlDbType.Int).Value = ID;
                    cmd.Parameters.Add("@UpdateBy", SqlDbType.NVarChar, 50).Value = (object?)maintenance.WorkDayId ?? DBNull.Value;
                    cmd.Parameters.Add("@UpdateTime", SqlDbType.DateTime).Value = DateTime.Now;

                    cmd.Parameters.Add("@StsMainID", SqlDbType.Int).Value = (object?)maintenance.StsMainID ?? DBNull.Value;
                    cmd.Parameters.Add("@MaintDate", SqlDbType.DateTime).Value = (object?)maintenance.MaintDate ?? DBNull.Value;
                    cmd.Parameters.Add("@VendorID", SqlDbType.Int).Value = (object?)maintenance.VendorID ?? DBNull.Value;
                    cmd.Parameters.Add("@PICID", SqlDbType.NVarChar).Value = (object?)maintenance.PICID ?? DBNull.Value;

                    var priceParam = cmd.Parameters.Add("@MaintPrice", SqlDbType.Decimal);
                    priceParam.Precision = 18;
                    priceParam.Scale = 2;
                    priceParam.Value = (object?)maintenance.MaintPrice ?? DBNull.Value;

                    cmd.Parameters.Add("@MaintPIC", SqlDbType.NVarChar, 50).Value = (object?)maintenance.MaintPIC ?? DBNull.Value;
                    cmd.Parameters.Add("@MaintDescription", SqlDbType.NVarChar, 255).Value = (object?)maintenance.MaintDescription ?? DBNull.Value;
                    cmd.Parameters.Add("@MaintNote", SqlDbType.NVarChar, 255).Value = (object?)maintenance.MaintNote ?? DBNull.Value;
                    cmd.Parameters.Add("@IsEQActive", SqlDbType.Bit).Value = (object?)maintenance.IsEQActive ?? DBNull.Value;
                    await cmd.ExecuteNonQueryAsync();
                }

                await using (var cmd2 = new SqlCommand(sqlUpdateEquipment, con, (SqlTransaction)tran))
                {
                    cmd2.Parameters.Add("@EQID", SqlDbType.Int).Value = ID;
                    cmd2.Parameters.Add("@MaintDate", SqlDbType.DateTime).Value = (object?)maintenance.MaintDate ?? DBNull.Value;
                    cmd2.Parameters.Add("@IsActive", SqlDbType.Bit).Value = (object?)maintenance.IsEQActive ?? DBNull.Value; // Giả sử sau bảo trì thì thiết bị không còn hoạt động

                    await cmd2.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
                return true;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }
        //public async Task<bool> CreatedAsync(MaintenanceDto maintenance, int ID)
        //{
        //    var connStr = _config.GetConnectionString("DefaultConnection");
        //    const string sqlCreateMaintenance = @"
        //    INSERT INTO dbo.Tbl_MaintenanceRecord 
        //        (EQID, UpdateBy, UpdateTime, StsMainID, MaintDate, VendorID, MaintPrice, PICID, MaintPIC, MaintDescription, MaintNote)
        //    VALUES 
        //        (@EQID, @UpdateBy, @UpdateTime, @StsMainID, @MaintDate, @VendorID, @MaintPrice, @PICID, @MaintPIC, @MaintDescription, @MaintNote)";

        //    const string sqlUpdateEquipment = @"
        //        UPDATE dbo.Equipment
        //        SET LastMaintenanceDate = @MaintDate
        //        WHERE ID = @EQID";

        //    await using var con = new SqlConnection(connStr);
        //    await using var cmd = new SqlCommand(sqlCreateMaintenance, con);


        //    cmd.Parameters.Add("@EQID", SqlDbType.Int).Value = (object?)ID ?? DBNull.Value;
        //    cmd.Parameters.Add("@UpdateBy", SqlDbType.NVarChar, 50).Value = (object?)maintenance.UpdateBy ?? DBNull.Value;
        //    cmd.Parameters.Add("@UpdateTime", SqlDbType.DateTime).Value = DateTime.Now;
        //    cmd.Parameters.Add("@StsMainID", SqlDbType.Int).Value = (object?)maintenance.StsMainID ?? DBNull.Value;
        //    cmd.Parameters.Add("@MaintDate", SqlDbType.DateTime).Value = (object?)maintenance.MaintDate ?? DBNull.Value;
        //    cmd.Parameters.Add("@VendorID", SqlDbType.Int).Value = (object?)maintenance.VendorID ?? DBNull.Value;
        //    cmd.Parameters.Add("@PICID", SqlDbType.Int).Value = (object?)maintenance.PICID ?? DBNull.Value;
        //    cmd.Parameters.Add("@MaintPrice", SqlDbType.Decimal).Value = (object?)maintenance.MaintPrice ?? DBNull.Value;
        //    cmd.Parameters.Add("@MaintPIC", SqlDbType.NVarChar, 50).Value = (object?)maintenance.MaintPIC ?? DBNull.Value;
        //    cmd.Parameters.Add("@MaintDescription", SqlDbType.NVarChar, 255).Value = (object?)maintenance.MaintDescription ?? DBNull.Value;
        //    cmd.Parameters.Add("@MaintNote", SqlDbType.NVarChar, 255).Value = (object?)maintenance.MaintNote ?? DBNull.Value;

        //    await using var cmd2 = new SqlCommand(sqlUpdateEquipment, con);


        //    cmd2.Parameters.Add("@EQID", SqlDbType.Int).Value = ID;
        //    cmd2.Parameters.Add("@LastMaintenanceDate", SqlDbType.DateTime).Value = (object?)maintenance.MaintDate ?? DBNull.Value;

        //    await con.OpenAsync();
        //    var result = await cmd.ExecuteNonQueryAsync();
        //    return result > 0;
        //}
    }
}
