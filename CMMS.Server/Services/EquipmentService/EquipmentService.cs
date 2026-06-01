using CMMS.Data.Connection;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CMMS.Server.Services.EquipmentService
{
    public class EquipmentService : IEquipmentService
    {
        private readonly IConfiguration _config;
        private readonly ISqlConnectionFactory _connectionFactory;
        public EquipmentService(IConfiguration config, ISqlConnectionFactory connectionFactory)
        {
            _config = config;
            _connectionFactory = connectionFactory;
        }
        public async Task<List<EquipmentDto>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM vw_EquipmentInfo WHERE IsActive = 1";

            var result = await connection.QueryAsync<EquipmentDto>(sql);
            return result.ToList();
        }
        public async Task<bool> CreatedAsync(EquipmentDto equipment)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            const string sql = @"
            INSERT INTO dbo.Tbl_EquipmentInfo 
                (EquipmentName, EquipmentCode, EquipmentBarcode, EquipmentModel, EquipmentSerial, EquipmentDescription, EquipmentNote, DeptId, BuyDate, BuyPrice, BuyCurrency, MaintenanceCircleTime, ContactNo, SAPCode, PICID, PIC, VendorID, LastMaintenanceDate, StsUseID, IsActive)
            VALUES 
                (@EquipmentName, @EquipmentCode, @EquipmentBarcode, @EquipmentModel, @EquipmentSerial, @EquipmentDescription, @EquipmentNote, @DeptId, @BuyDate, @BuyPrice, @BuyCurrency, @MaintenanceCircleTime, @ContactNo, @SAPCode, @PICID, @PIC, @VendorID, @LastMaintenanceDate, @StsUseID, @IsActive)
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
            cmd.Parameters.Add("@DeptId", SqlDbType.Int).Value = (object?)equipment.DeptID ?? DBNull.Value;
            cmd.Parameters.Add("@BuyDate", SqlDbType.DateTime).Value = (object?)equipment.BuyDate ?? DBNull.Value;
            cmd.Parameters.Add("@BuyPrice", SqlDbType.Decimal).Value = (object?)equipment.BuyPrice ?? DBNull.Value;
            cmd.Parameters.Add("@BuyCurrency", SqlDbType.NVarChar).Value = (object?)equipment.BuyCurrency ?? DBNull.Value;
            cmd.Parameters.Add("@MaintenanceCircleTime", SqlDbType.Int).Value = (object?)equipment.MaintenanceCircleTime ?? DBNull.Value;
            cmd.Parameters.Add("@ContactNo", SqlDbType.NVarChar).Value = (object?)equipment.ContactNo ?? DBNull.Value;
            cmd.Parameters.Add("@SAPCode", SqlDbType.Int).Value = (object?)equipment.SAPCode ?? DBNull.Value;
            cmd.Parameters.Add("@PICID", SqlDbType.UniqueIdentifier).Value = (object?)equipment.PICID ?? DBNull.Value;
            cmd.Parameters.Add("@PIC", SqlDbType.NVarChar).Value = (object?)equipment.PIC ?? DBNull.Value;
            cmd.Parameters.Add("@VendorID", SqlDbType.Int).Value = (object?)equipment.VendorID ?? DBNull.Value;
            cmd.Parameters.Add("@LastMaintenanceDate", SqlDbType.DateTime).Value = (object?)equipment.BuyDate ?? DBNull.Value;
            cmd.Parameters.Add("@StsUseID", SqlDbType.Int).Value = (object?)equipment.StsUseID ?? DBNull.Value;
            cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = (object?)equipment.IsActive ?? DBNull.Value;
            await con.OpenAsync();
            var result = await cmd.ExecuteNonQueryAsync();
            return result > 0;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            await using var connection = (SqlConnection)_connectionFactory.CreateConnection();

            const string sql = @" 
            DELETE FROM dbo.Tbl_EquipmentInfo
            WHERE EQID = @EQID
        ";
            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@EQID", SqlDbType.Int).Value = id;
            await connection.OpenAsync();
            var result = await cmd.ExecuteNonQueryAsync();
            return result > 0;
        }
        public async Task<bool> UpdateAsync(EquipmentDto equipment)
        {
            try
            {
                await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
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
                        DeptID = @DeptID,
                        StsUseID = @StsUseID,
                        VendorID = @VendorID,
                        MaintenanceCircleTime = @MaintenanceCircleTime,
                        BuyDate = @BuyDate,
                        BuyPrice = @BuyPrice,
                        BuyCurrency = @BuyCurrency,
                        ContactNo = @ContactNo,
                        SAPCode = @SAPCode,
                        PIC = @PIC,
                        IsActive = @IsActive
                    WHERE EQID = @EQID
                ";
                await using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.Add("@EQID", SqlDbType.Int).Value = equipment.EQID;
                cmd.Parameters.Add("@EquipmentName", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentName ?? DBNull.Value;
                cmd.Parameters.Add("@EquipmentCode", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentCode ?? DBNull.Value;
                cmd.Parameters.Add("@EquipmentBarcode", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentBarcode ?? DBNull.Value;
                cmd.Parameters.Add("@EquipmentModel", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentModel ?? DBNull.Value;
                cmd.Parameters.Add("@EquipmentSerial", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentSerial ?? DBNull.Value;
                cmd.Parameters.Add("@EquipmentDescription", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentDescription ?? DBNull.Value;
                cmd.Parameters.Add("@EquipmentNote", SqlDbType.NVarChar).Value = (object?)equipment.EquipmentNote ?? DBNull.Value;
                cmd.Parameters.Add("@DeptId", SqlDbType.Int).Value = (object?)equipment.DeptID ?? DBNull.Value;
                cmd.Parameters.Add("@BuyDate", SqlDbType.DateTime).Value = (object?)equipment.BuyDate ?? DBNull.Value;
                cmd.Parameters.Add("@BuyPrice", SqlDbType.Decimal).Value = (object?)equipment.BuyPrice ?? DBNull.Value;
                cmd.Parameters.Add("@BuyCurrency", SqlDbType.NVarChar).Value = (object?)equipment.BuyCurrency ?? DBNull.Value;
                cmd.Parameters.Add("@MaintenanceCircleTime", SqlDbType.Int).Value = (object?)equipment.MaintenanceCircleTime ?? DBNull.Value;
                cmd.Parameters.Add("@ContactNo", SqlDbType.NVarChar).Value = (object?)equipment.ContactNo ?? DBNull.Value;
                cmd.Parameters.Add("@SAPCode", SqlDbType.Int).Value = (object?)equipment.SAPCode ?? DBNull.Value;
                cmd.Parameters.Add("@PIC", SqlDbType.NVarChar).Value = (object?)equipment.PIC ?? DBNull.Value;
                cmd.Parameters.Add("@VendorID", SqlDbType.Int).Value = (object?)equipment.VendorID ?? DBNull.Value;
                cmd.Parameters.Add("@StsUseID", SqlDbType.Int).Value = (object?)equipment.StsUseID ?? DBNull.Value;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = (object?)equipment.IsActive ?? DBNull.Value;
                await connection.OpenAsync();
                var result = await cmd.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while updating equipment: {ex.Message}");
                return false;

            }
        }
        //public async Task<ApiResponse> RequestScrapAsync(int eqId)
        //{
        //    try
        //    {
        //        using var connection =
        //            _connectionFactory.CreateConnection();

        //        // tìm equipment
        //        var equipment =
        //            await connection.QueryFirstOrDefaultAsync<EquipmentDto>(
        //                @"SELECT *
        //              FROM dbo.Tbl_EquipmentInfo
        //              WHERE EQID = @EQID",
        //                new
        //                {
        //                    EQID = eqId
        //                });

        //        if (equipment == null)
        //        {
        //            return new ApiResponse
        //            {
        //                Success = false,
        //                Message = "Không tìm thấy thiết bị"
        //            };
        //        }

        //        //// kiểm tra status
        //        //if (equipment.StsMainID == "Pending")
        //        //{
        //        //    return new ApiResponse
        //        //    {
        //        //        Success = false,
        //        //        Message = "Thiết bị đang chờ approve"
        //        //    };
        //        //}

        //        // update equipment
        //        await connection.ExecuteAsync(
        //            @"UPDATE dbo.Tbl_EquipmentInfo
        //          SET StsUseID = 4
        //          WHERE EQID = @EQID",
        //            new
        //            {
        //                EQID = eqId
        //            });

        //        // insert scrap request
        //        var requestId =
        //            await connection.ExecuteScalarAsync<int>(
        //                @"INSERT INTO dbo.Tbl_ScrapRequest
        //              (
        //                  EQID,
        //                  RequestDate,
        //                  RequestStatus
        //              )
        //              VALUES
        //              (
        //                  @EQID,
        //                  GETDATE(),
        //                  'Pending'
        //              );

        //              SELECT CAST(SCOPE_IDENTITY() as int)",
        //                new
        //                {
        //                    EQID = eqId
        //                });

        //        // power automate
        //        var flowUrl =
        //            _config["PowerAutomate:ScrapFlowUrl"];

        //        using var client = new HttpClient();

        //        var payload = new
        //        {
        //            RequestId = requestId,
        //            EQID = equipment.EQID,
        //            EquipmentName = equipment.EquipmentName
        //        };

        //        var response =
        //            await client.PostAsJsonAsync(
        //                flowUrl,
        //                payload);
        //        var responseContent = await response.Content.ReadAsStringAsync();
        //        if (!response.IsSuccessStatusCode)
        //        {
        //            return new ApiResponse
        //            {
        //                Success = false,
        //                Message = "Không gửi được approval"
        //            };
        //        }

        //        return new ApiResponse
        //        {
        //            Success = true,
        //            Message = "Đã gửi yêu cầu approve"
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse
        //        {
        //            Success = false,
        //            Message = ex.Message
        //        };
        //    }
        //}
    }
}