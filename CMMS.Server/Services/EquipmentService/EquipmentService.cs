using CMMS.Shared.EquipmentDto;
using CMMS.Server.Services.EquipmentService;
using Microsoft.Data.SqlClient;
using System.Data;

public class EquipmentService : IEquipmentService
{
    private readonly IConfiguration _config;

    public EquipmentService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<List<EquipmentDto>> GetAllAsync()
    {
        try
        {
            var list = new List<EquipmentDto>();
            var connStr = _config.GetConnectionString("DefaultConnection");

            const string sql = @"
                    SELECT *
                    FROM dbo.vw_EquipmentInfo";

            using var con = new SqlConnection(connStr);
            using var cmd = new SqlCommand(sql, con);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {

                list.Add(new EquipmentDto
                {
                    EquipmentName = reader["EquipmentName"]?.ToString() ?? "",
                    EquipmentBarcode = reader["EquipmentBarcode"]?.ToString(),
                    EquipmentCode = reader["EquipmentCode"]?.ToString(),
                    EquipmentModel = reader["EquipmentModel"]?.ToString(),
                    EquipmentSerial = reader["EquipmentSerial"]?.ToString(),
                    EquipmentDescription = reader["EquipmentDescription"]?.ToString(),
                    EquipmentNote = reader["EquipmentNote"]?.ToString(),
                    Location = reader["Location"]?.ToString(),
                    Status = reader["Status"]?.ToString(),
                    BuyDate = reader["BuyDate"] as DateTime?,
                    BuyPrice = reader["BuyPrice"]?.ToString(),
                    BuyCurrency = reader["BuyCurrency"]?.ToString(),
                    ContactNo = reader["ContactNo"]?.ToString(),
                    SAPCode = reader["SAPCode"]?.ToString()
                    //Id = reader.GetInt32(0),
                    //EquipmentName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    //EquipmentBarcode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    //EquipmentCode = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    //EquipmentModel = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    //EquipmentSerial = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    //EquipmentDescription = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    //EquipmentNote = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    //BuyDate = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8),
                    //BuyCurrency = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
                });
            }

            return list;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching equipment data: {ex.Message}");
            return new List<EquipmentDto>();
        }
    }
    public async Task<bool> CreatedAsync (EquipmentDto equipment)
    {
        var connStr = _config.GetConnectionString("DefaultConnection");

        const string sql = @"
            INSERT INTO dbo.Tbl_EquipmentInfo 
                (EquipmentName, EquipmentCode, EquipmentBarcode, EquipmentModel, EquipmentSerial, EquipmentDescription, EquipmentNote, DeptId, BuyDate, BuyPrice, BuyCurrency, MaintenanceCircleTime, ContactNo, SAPCode, PIC)
            VALUES 
                (@EquipmentName, @EquipmentCode, @EquipmentBarcode, @EquipmentModel, @EquipmentSerial, @EquipmentDescription, @EquipmentNote, @DeptId, @BuyDate, @BuyPrice, @BuyCurrency, @MaintenanceCircleTime, @ContactNo, @SAPCode, @PIC)
        ";
        await using var con = new SqlConnection(connStr);
        await using var cmd = new SqlCommand(sql, con);

        cmd.Parameters.Add("@EquipmentName", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentName ?? DBNull.Value;
        cmd.Parameters.Add("@EquipmentCode", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentCode ?? DBNull.Value;
        cmd.Parameters.Add("@EquipmentBarcode", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentBarcode ?? DBNull.Value;
        cmd.Parameters.Add("@EquipmentModel", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentModel ?? DBNull.Value;
        cmd.Parameters.Add("@EquipmentSerial", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentSerial ?? DBNull.Value;  
        cmd.Parameters.Add("@EquipmentDescription", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentDescription ?? DBNull.Value;
        cmd.Parameters.Add("@EquipmentNote", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentNote ?? DBNull.Value;
        cmd.Parameters.Add("@DeptId", SqlDbType.Int).Value = (object?)equipment.DeptId ?? DBNull.Value;
        cmd.Parameters.Add("@BuyDate", SqlDbType.DateTime).Value = (object?)equipment.BuyDate ?? DBNull.Value;
        cmd.Parameters.Add("@BuyPrice", SqlDbType.NVarChar).Value = (object?)equipment.BuyPrice ?? DBNull.Value;
        cmd.Parameters.Add("@BuyCurrency", SqlDbType.NVarChar).Value = (object?)equipment.BuyCurrency ?? DBNull.Value;
        cmd.Parameters.Add("@MaintenanceCircleTime", SqlDbType.Int).Value = (object?)equipment.MaintenanceCircleTime ?? DBNull.Value;
        cmd.Parameters.Add("@ContactNo", SqlDbType.NVarChar).Value = (object?)equipment.ContactNo ?? DBNull.Value;
        cmd.Parameters.Add("@SAPCode", SqlDbType.Int).Value = (object?)equipment.SAPCode ?? DBNull.Value;
        cmd.Parameters.Add("@PIC", SqlDbType.NVarChar).Value = (object?)equipment.PIC ?? DBNull.Value;

        await con.OpenAsync();
        var result = await cmd.ExecuteNonQueryAsync();
        return result > 0;
    }
    public async Task<bool> DeleteAsync(int id)
    {
        var connStr = _config.GetConnectionString("DefaultConnection");
        const string sql = @" 
            DELETE FROM dbo.Tbl_EquipmentInfo
            WHERE Id = @Id
        ";
        await using var con = new SqlConnection(connStr);
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await con.OpenAsync();
        var result = await cmd.ExecuteNonQueryAsync();
        return result > 0;
    }
    public async Task<bool> UpdateAsync(EquipmentDto equipment)
    {
        var connStr = _config.GetConnectionString("DefaultConnection");
        const string sql = @"
            UPDATE dbo.Tbl_EquipmentInfo
            SET 
                EquipmentName = @EquipmentName,
                EquipmentCode = @EquipmentCode,
                EquipmentBarcode = @EquipmentBarcode,
                EquipmentModel = @EquipmentModel,
                EquipmentSerial = @EquipmentSerial,
                EquipmentDescription = @EquipmentDescription,
                EquipmentNote = @EquipmentNote,
                Location = @Location,
                Status = @Status,
                BuyDate = @BuyDate,
                BuyPrice = @BuyPrice,
                BuyCurrency = @BuyCurrency,
                ContactNo = @ContactNo,
                SAPCode = @SAPCode
            WHERE Id = @Id
        ";
        await using var con = new SqlConnection(connStr);
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add("@EquipmentName", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentName ?? DBNull.Value;
        cmd.Parameters.Add("@EquipmentModel", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentModel ?? DBNull.Value;
        cmd.Parameters.Add("@EquipmentSerial", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentSerial ?? DBNull.Value;
        cmd.Parameters.Add("@Location", SqlDbType.NVarChar).Value = (object?)equipment.Location ?? DBNull.Value;
        cmd.Parameters.Add("@Status", SqlDbType.NVarChar).Value = (object?)equipment.Status ?? DBNull.Value;
        cmd.Parameters.Add("@BuyDate", SqlDbType.DateTime).Value = (object?)equipment.BuyDate ?? DBNull.Value;
        await con.OpenAsync();
        var result = await cmd.ExecuteNonQueryAsync();
        return result > 0;
    }
}
