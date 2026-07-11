using CMMS.Data.Connection;
using CMMS.Shared.Authorization;
using CMMS.Shared.Dtos.Common;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.User;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;

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
        public async Task<List<EquipmentDto>> GetAllAsync(int? factoryId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM vw_EquipmentInfo WHERE IsActive = 1";
            if (factoryId.HasValue)
            {
                sql += " AND FACID = @FactoryId";
            }
            var result = await connection.QueryAsync<EquipmentDto>(sql, new { FactoryId = factoryId });
            return result.ToList();
        }
        public async Task<bool> CreatedAsync(EquipmentDto equipment)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            const string sql = @"
            INSERT INTO dbo.Tbl_EquipmentInfo 
                (EquipmentName, EquipmentCode, EquipmentBarcode, EquipmentModel, EquipmentSerial, EquipmentDescription, EquipmentNote, DeptId, LocID, BuyDate, BuyPrice, BuyCurrency, MaintenanceCircleTime, ContactNo, SAPCode, PICID, PIC, VendorID, LastMaintenanceDate, StsUseID, IsActive)
            VALUES 
                (@EquipmentName, @EquipmentCode, @EquipmentBarcode, @EquipmentModel, @EquipmentSerial, @EquipmentDescription, @EquipmentNote, @DeptId, @LocID, @BuyDate, @BuyPrice, @BuyCurrency, @MaintenanceCircleTime, @ContactNo, @SAPCode, @PICID, @PIC, @VendorID, @LastMaintenanceDate, @StsUseID, @IsActive)
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
            cmd.Parameters.Add("@LocID", SqlDbType.Int).Value = (object?)equipment.LocID ?? DBNull.Value;
            cmd.Parameters.Add("@BuyDate", SqlDbType.DateTime).Value = (object?)equipment.BuyDate ?? DBNull.Value;
            cmd.Parameters.Add("@BuyPrice", SqlDbType.Decimal).Value = (object?)equipment.BuyPrice ?? DBNull.Value;
            cmd.Parameters.Add("@BuyCurrency", SqlDbType.NVarChar).Value = (object?)equipment.BuyCurrency ?? DBNull.Value;
            cmd.Parameters.Add("@MaintenanceCircleTime", SqlDbType.Int).Value = (object?)equipment.MaintenanceCircleTime ?? DBNull.Value;
            cmd.Parameters.Add("@ContactNo", SqlDbType.NVarChar).Value = (object?)equipment.ContactNo ?? DBNull.Value;
            cmd.Parameters.Add("@SAPCode", SqlDbType.Int).Value = (object?)equipment.SAPCode ?? DBNull.Value;
            cmd.Parameters.Add("@PICID", SqlDbType.NVarChar).Value = (object?)equipment.PICID ?? DBNull.Value;
            cmd.Parameters.Add("@PIC", SqlDbType.NVarChar).Value = (object?)equipment.PIC ?? DBNull.Value;
            cmd.Parameters.Add("@VendorID", SqlDbType.Int).Value = (object?)equipment.VendorID ?? DBNull.Value;
            cmd.Parameters.Add("@LastMaintenanceDate", SqlDbType.DateTime).Value = (object?)equipment.BuyDate ?? DBNull.Value;
            cmd.Parameters.Add("@StsUseID", SqlDbType.Int).Value = (object?)equipment.StsUseID ?? DBNull.Value;
            cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = (object?)equipment.IsActive ?? DBNull.Value;
            await con.OpenAsync();
            var result = await cmd.ExecuteNonQueryAsync();
            return result > 0;
        }
        public async Task<bool> DeleteAsync(int id, UserDto currentUser)
        {
            await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string selectSql = "SELECT FACID, PICID FROM dbo.vw_EquipmentInfo WHERE EQID = @EQID";
            var original = await connection.QueryFirstOrDefaultAsync<EquipmentDto>(selectSql, new { EQID = id });

            if (original == null)
                throw new KeyNotFoundException($"Không tìm thấy thiết bị EQID = {id}.");

            if (!AuthorizationHelper.CanEditOrMaintain(currentUser, original.FACID, original.PICID))
                throw new UnauthorizedAccessException("Bạn không có quyền xóa thiết bị này.");

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
        public async Task<bool> UpdateAsync(EquipmentDto equipment, UserDto currentUser)
        {
            await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();

            // Lấy FACID và PICID gốc từ DB để check quyền
            const string selectSql = "SELECT FACID, PICID FROM dbo.vw_EquipmentInfo WHERE EQID = @EQID";
            var original = await connection.QueryFirstOrDefaultAsync<EquipmentDto>(selectSql, new { equipment.EQID });

            if (original == null)
                throw new KeyNotFoundException($"Không tìm thấy thiết bị EQID = {equipment.EQID}.");

            if (!AuthorizationHelper.CanEditOrMaintain(currentUser, original.FACID, original.PICID))
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa thiết bị này.");

            try
            {
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
                        DeptId = @DeptId,
                        LocID = @LocID, 
                        StsUseID = @StsUseID,
                        VendorID = @VendorID,
                        MaintenanceCircleTime = @MaintenanceCircleTime,
                        BuyDate = @BuyDate,
                        BuyPrice = @BuyPrice,
                        BuyCurrency = @BuyCurrency,
                        ContactNo = @ContactNo,
                        SAPCode = @SAPCode,
                        PIC = @PIC,
                        PICID = @PICID,
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
                cmd.Parameters.Add("@LocID", SqlDbType.Int).Value = (object?)equipment.LocID ?? DBNull.Value;
                cmd.Parameters.Add("@BuyDate", SqlDbType.DateTime).Value = (object?)equipment.BuyDate ?? DBNull.Value;
                cmd.Parameters.Add("@BuyPrice", SqlDbType.Decimal).Value = (object?)equipment.BuyPrice ?? DBNull.Value;
                cmd.Parameters.Add("@BuyCurrency", SqlDbType.NVarChar).Value = (object?)equipment.BuyCurrency ?? DBNull.Value;
                cmd.Parameters.Add("@MaintenanceCircleTime", SqlDbType.Int).Value = (object?)equipment.MaintenanceCircleTime ?? DBNull.Value;
                cmd.Parameters.Add("@ContactNo", SqlDbType.NVarChar).Value = (object?)equipment.ContactNo ?? DBNull.Value;
                cmd.Parameters.Add("@SAPCode", SqlDbType.Int).Value = (object?)equipment.SAPCode ?? DBNull.Value;
                cmd.Parameters.Add("@PIC", SqlDbType.NVarChar).Value = (object?)equipment.PIC ?? DBNull.Value;
                cmd.Parameters.Add("@PICID", SqlDbType.NVarChar).Value = (object?)equipment.PICID ?? DBNull.Value;
                cmd.Parameters.Add("@VendorID", SqlDbType.Int).Value = (object?)equipment.VendorID ?? DBNull.Value;
                cmd.Parameters.Add("@StsUseID", SqlDbType.Int).Value = (object?)equipment.StsUseID ?? DBNull.Value;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = (object?)equipment.IsActive ?? DBNull.Value;
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
        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var inQuotes = false;
            var currentField = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            result.Add(currentField.ToString().Trim());
            return result;
        }

        public async Task<ImportResultDto> ImportEquipmentsAsync(Stream fileStream, string fileName, UserDto currentUser)
        {
            var result = new ImportResultDto();
            try
            {
                using var reader = new StreamReader(fileStream);
                var content = await reader.ReadToEndAsync();
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length <= 1)
                {
                    result.Success = false;
                    result.Message = "File import không có dữ liệu hoặc sai định dạng.";
                    return result;
                }

                var headerLine = lines[0];
                var headers = ParseCsvLine(headerLine);
                
                int colCode = headers.FindIndex(h => h.Equals("EquipmentCode", StringComparison.OrdinalIgnoreCase));
                int colName = headers.FindIndex(h => h.Equals("EquipmentName", StringComparison.OrdinalIgnoreCase));
                int colModel = headers.FindIndex(h => h.Equals("EquipmentModel", StringComparison.OrdinalIgnoreCase));
                int colSerial = headers.FindIndex(h => h.Equals("EquipmentSerial", StringComparison.OrdinalIgnoreCase));
                int colDesc = headers.FindIndex(h => h.Equals("EquipmentDescription", StringComparison.OrdinalIgnoreCase));
                int colNote = headers.FindIndex(h => h.Equals("EquipmentNote", StringComparison.OrdinalIgnoreCase));
                int colLoc = headers.FindIndex(h => h.Equals("LocCode", StringComparison.OrdinalIgnoreCase));
                int colDept = headers.FindIndex(h => h.Equals("DeptCode", StringComparison.OrdinalIgnoreCase));
                int colBuyDate = headers.FindIndex(h => h.Equals("BuyDate", StringComparison.OrdinalIgnoreCase));
                int colBuyPrice = headers.FindIndex(h => h.Equals("BuyPrice", StringComparison.OrdinalIgnoreCase));
                int colBuyCurrency = headers.FindIndex(h => h.Equals("BuyCurrency", StringComparison.OrdinalIgnoreCase));
                int colMaintCircle = headers.FindIndex(h => h.Equals("MaintenanceCircleTime", StringComparison.OrdinalIgnoreCase));
                int colContact = headers.FindIndex(h => h.Equals("ContactNo", StringComparison.OrdinalIgnoreCase));
                int colSap = headers.FindIndex(h => h.Equals("SAPCode", StringComparison.OrdinalIgnoreCase));
                int colPic = headers.FindIndex(h => h.Equals("PIC", StringComparison.OrdinalIgnoreCase));

                if (colCode == -1 || colName == -1)
                {
                    result.Success = false;
                    result.Message = "File import thiếu cột bắt buộc: EquipmentCode, EquipmentName.";
                    return result;
                }

                using var connection = _connectionFactory.CreateConnection();
                connection.Open();

                var locations = (await connection.QueryAsync<LocationDto>("SELECT LocID, LocCode, LocName FROM dbo.Tbl_FactoryLocation")).ToList();
                var departments = (await connection.QueryAsync<DepartmentDto>("SELECT DeptID, DeptCode FROM dbo.vw_FactoryDepartment")).ToList();
                var users = (await connection.QueryAsync<UserDto>("SELECT Id, WorkDayId, FullName FROM dbo.Tbl_User")).ToList();

                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var fields = ParseCsvLine(line);
                    if (fields.Count == 0 || fields.All(string.IsNullOrWhiteSpace)) continue;

                    while (fields.Count < headers.Count) fields.Add("");

                    try
                    {
                        var code = fields[colCode].Trim();
                        var name = fields[colName].Trim();

                        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                        {
                            result.FailureCount++;
                            result.Errors.Add($"Dòng {i + 1}: Mã hoặc tên thiết bị không được trống.");
                            continue;
                        }

                        var model = colModel != -1 ? fields[colModel].Trim() : "";
                        var serial = colSerial != -1 ? fields[colSerial].Trim() : "";
                        var desc = colDesc != -1 ? fields[colDesc].Trim() : "";
                        var note = colNote != -1 ? fields[colNote].Trim() : "";
                        var contact = colContact != -1 ? fields[colContact].Trim() : "";
                        
                        DateTime? buyDate = null;
                        if (colBuyDate != -1 && DateTime.TryParse(fields[colBuyDate], out var parsedDate)) buyDate = parsedDate;

                        decimal? buyPrice = null;
                        if (colBuyPrice != -1 && decimal.TryParse(fields[colBuyPrice], out var parsedPrice)) buyPrice = parsedPrice;

                        var buyCurrency = (colBuyCurrency != -1 && !string.IsNullOrEmpty(fields[colBuyCurrency])) ? fields[colBuyCurrency].Trim() : "VND";

                        int? maintCircle = null;
                        if (colMaintCircle != -1 && int.TryParse(fields[colMaintCircle], out var parsedMaint)) maintCircle = parsedMaint;

                        int? sapCode = null;
                        if (colSap != -1 && int.TryParse(fields[colSap], out var parsedSap)) sapCode = parsedSap;

                        string? picName = null;
                        string? picId = null;
                        if (colPic != -1)
                        {
                            var picValue = fields[colPic].Trim();
                            if (!string.IsNullOrEmpty(picValue))
                            {
                                var u = users.FirstOrDefault(x => x.FullName.Equals(picValue, StringComparison.OrdinalIgnoreCase) || x.WorkDayId.Equals(picValue, StringComparison.OrdinalIgnoreCase));
                                if (u != null)
                                {
                                    picName = u.FullName;
                                    picId = u.WorkDayId;
                                }
                                else
                                {
                                    picName = picValue;
                                }
                            }
                        }

                        int? locId = null;
                        if (colLoc != -1)
                        {
                            var locCodeVal = fields[colLoc].Trim();
                            if (!string.IsNullOrEmpty(locCodeVal))
                            {
                                var loc = locations.FirstOrDefault(l => l.LocCode != null && l.LocCode.Equals(locCodeVal, StringComparison.OrdinalIgnoreCase));
                                if (loc != null) locId = loc.LocID;
                            }
                        }

                        int? deptId = null;
                        if (colDept != -1)
                        {
                            var deptCodeVal = fields[colDept].Trim();
                            if (!string.IsNullOrEmpty(deptCodeVal))
                            {
                                var dept = departments.FirstOrDefault(d => d.DeptCode != null && d.DeptCode.Equals(deptCodeVal, StringComparison.OrdinalIgnoreCase));
                                if (dept != null) deptId = dept.DeptID;
                            }
                        }

                        var existingEq = await connection.QueryFirstOrDefaultAsync<int?>(
                            "SELECT EQID FROM dbo.Tbl_EquipmentInfo WHERE EquipmentCode = @EquipmentCode",
                            new { EquipmentCode = code });

                        if (existingEq.HasValue)
                        {
                            var updateSql = @"
                                UPDATE dbo.Tbl_EquipmentInfo
                                SET EquipmentName = @EquipmentName, EquipmentModel = @EquipmentModel, EquipmentSerial = @EquipmentSerial,
                                    EquipmentDescription = @EquipmentDescription, EquipmentNote = @EquipmentNote, DeptId = @DeptId, LocID = @LocID,
                                    BuyDate = @BuyDate, BuyPrice = @BuyPrice, BuyCurrency = @BuyCurrency, MaintenanceCircleTime = @MaintenanceCircleTime,
                                    ContactNo = @ContactNo, SAPCode = @SAPCode, PIC = @PIC, PICID = @PICID
                                WHERE EQID = @EQID";
                            
                            await connection.ExecuteAsync(updateSql, new {
                                EQID = existingEq.Value,
                                EquipmentName = name,
                                EquipmentModel = model,
                                EquipmentSerial = serial,
                                EquipmentDescription = desc,
                                EquipmentNote = note,
                                DeptId = deptId,
                                LocID = locId,
                                BuyDate = buyDate,
                                BuyPrice = buyPrice,
                                BuyCurrency = buyCurrency,
                                MaintenanceCircleTime = maintCircle,
                                ContactNo = contact,
                                SAPCode = sapCode,
                                PIC = picName,
                                PICID = picId
                            });
                        }
                        else
                        {
                            var insertSql = @"
                                INSERT INTO dbo.Tbl_EquipmentInfo 
                                    (EquipmentName, EquipmentCode, EquipmentModel, EquipmentSerial, EquipmentDescription, EquipmentNote, DeptId, LocID, BuyDate, BuyPrice, BuyCurrency, MaintenanceCircleTime, ContactNo, SAPCode, PIC, PICID, LastMaintenanceDate, StsUseID, IsActive)
                                VALUES 
                                    (@EquipmentName, @EquipmentCode, @EquipmentModel, @EquipmentSerial, @EquipmentDescription, @EquipmentNote, @DeptId, @LocID, @BuyDate, @BuyPrice, @BuyCurrency, @MaintenanceCircleTime, @ContactNo, @SAPCode, @PIC, @PICID, @LastMaintenanceDate, 1, 1)";
                            
                            await connection.ExecuteAsync(insertSql, new {
                                EquipmentName = name,
                                EquipmentCode = code,
                                EquipmentModel = model,
                                EquipmentSerial = serial,
                                EquipmentDescription = desc,
                                EquipmentNote = note,
                                DeptId = deptId,
                                LocID = locId,
                                BuyDate = buyDate,
                                BuyPrice = buyPrice,
                                BuyCurrency = buyCurrency,
                                MaintenanceCircleTime = maintCircle,
                                ContactNo = contact,
                                SAPCode = sapCode,
                                PIC = picName,
                                PICID = picId,
                                LastMaintenanceDate = buyDate
                            });
                        }

                        result.SuccessCount++;
                    }
                    catch (Exception rowEx)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Dòng {i + 1}: Lỗi - {rowEx.Message}");
                    }
                }

                result.Success = true;
                result.Message = $"Nhập dữ liệu hoàn tất. Thành công: {result.SuccessCount}, Thất bại: {result.FailureCount}.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Lỗi hệ thống khi import: {ex.Message}";
            }
            return result;
        }
    }
}