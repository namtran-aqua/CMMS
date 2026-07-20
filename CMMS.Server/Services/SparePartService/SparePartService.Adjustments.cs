using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartService
{
    public partial class SparePartService
    {
        public async Task<List<AdjustOrderDto>> GetAdjustOrdersAsync(int? factoryId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT o.AdjustID, o.AdjustCode, o.AdjustDate, o.FACID, o.Status, o.Note, o.CreateBy, o.CreateAt,
                       u.WorkDayId AS CreateUser
                FROM dbo.Tbl_AdjustOrder o
                LEFT JOIN dbo.Tbl_User u ON u.Id = o.CreateBy";
            
            if (factoryId.HasValue)
            {
                sql += " WHERE o.FACID = @FactoryId";
            }
            sql += " ORDER BY o.AdjustDate DESC, o.AdjustID DESC";

            var orders = (await connection.QueryAsync<AdjustOrderDto>(sql, new { FactoryId = factoryId })).ToList();

            // Load all detail lines
            var sqlDetails = @"
                SELECT d.DetailID, d.AdjustID, d.SPID, d.Type, d.HasCode, d.SerialCode, d.Quantity, d.BeforeQty, d.AfterQty, d.ItemID,
                       p.PartCode, p.PartName
                FROM dbo.Tbl_AdjustOrderDetail d
                JOIN dbo.Tbl_SparePart p ON p.SPID = d.SPID";
            
            var details = (await connection.QueryAsync<AdjustOrderDetailDto>(sqlDetails)).ToList();
            var detailsGrouped = details.GroupBy(d => d.AdjustID).ToDictionary(g => g.Key, g => g.ToList());

            // Load attachments
            var sqlAttachments = "SELECT ReferenceID, FilePath FROM dbo.Tbl_Attachments WHERE ReferenceType = 'Adjust'";
            var attachments = (await connection.QueryAsync<dynamic>(sqlAttachments)).ToList();
            var attachmentsGrouped = attachments.GroupBy(a => (string)a.ReferenceID).ToDictionary(g => g.Key, g => g.Select(x => (string)x.FilePath).ToList());

            foreach (var order in orders)
            {
                if (detailsGrouped.TryGetValue(order.AdjustID, out var lines))
                {
                    order.Lines = lines;
                    order.TotalLines = lines.Count;
                    order.NetAdjustment = lines.Sum(l => l.Type == "IN" ? l.Quantity : -l.Quantity);
                }
                else
                {
                    order.Lines = new List<AdjustOrderDetailDto>();
                    order.TotalLines = 0;
                    order.NetAdjustment = 0;
                }

                if (attachmentsGrouped.TryGetValue(order.AdjustID.ToString(), out var urls))
                {
                    order.AttachmentUrls = urls;
                }
                else
                {
                    order.AttachmentUrls = new List<string>();
                }
            }

            return orders;
        }

        public async Task<AdjustOrderDto> GetAdjustOrderByIdAsync(int adjustId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT o.AdjustID, o.AdjustCode, o.AdjustDate, o.FACID, o.Status, o.Note, o.CreateBy, o.CreateAt,
                       u.WorkDayId AS CreateUser
                FROM dbo.Tbl_AdjustOrder o
                LEFT JOIN dbo.Tbl_User u ON u.Id = o.CreateBy
                WHERE o.AdjustID = @AdjustID";

            var order = await connection.QueryFirstOrDefaultAsync<AdjustOrderDto>(sql, new { AdjustID = adjustId });
            if (order == null) return null;

            var sqlDetails = @"
                SELECT d.DetailID, d.AdjustID, d.SPID, d.Type, d.HasCode, d.SerialCode, d.Quantity, d.BeforeQty, d.AfterQty, d.ItemID,
                       p.PartCode, p.PartName
                FROM dbo.Tbl_AdjustOrderDetail d
                JOIN dbo.Tbl_SparePart p ON p.SPID = d.SPID
                WHERE d.AdjustID = @AdjustID";

            order.Lines = (await connection.QueryAsync<AdjustOrderDetailDto>(sqlDetails, new { AdjustID = adjustId })).ToList();
            order.TotalLines = order.Lines.Count;
            order.NetAdjustment = order.Lines.Sum(l => l.Type == "IN" ? l.Quantity : -l.Quantity);

            var sqlAttachments = "SELECT FilePath FROM dbo.Tbl_Attachments WHERE ReferenceType = 'Adjust' AND ReferenceID = @ReferenceID";
            order.AttachmentUrls = (await connection.QueryAsync<string>(sqlAttachments, new { ReferenceID = adjustId.ToString() })).ToList();

            return order;
        }

        public async Task<AdjustOrderDto> CreateAdjustOrderAsync(CreateAdjustOrderDto dto, UserDto currentUser)
        {
            if (dto.Lines == null || !dto.Lines.Any())
                throw new ArgumentException("Lệnh điều chỉnh phải chứa ít nhất một mặt hàng.");


            var connStr = _config.GetConnectionString("DefaultConnection");

            await using var con = new SqlConnection(connStr);
            await con.OpenAsync();
            await using var tran = await con.BeginTransactionAsync();

            try
            {
                var countToday = await con.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM dbo.Tbl_AdjustOrder WHERE CAST(CreateAt AS DATE) = CAST(GETDATE() AS DATE)",
                    transaction: (SqlTransaction)tran);
                var adjustCode = $"ADJ-{DateTime.Now:yyMMdd}-{(countToday + 1):D4}";
                // 1. Insert AdjustOrder header
                const string sqlHeader = @"
                    INSERT INTO dbo.Tbl_AdjustOrder (AdjustCode, AdjustDate, FACID, Status, Note, CreateBy, CreateAt)
                    VALUES (@AdjustCode, @AdjustDate, @FACID, 'Completed', @Note, @CreateBy, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() as int);";
                
                int adjustId;
                await using (var cmdHeader = new SqlCommand(sqlHeader, con, (SqlTransaction)tran))
                {
                    cmdHeader.Parameters.Add("@AdjustCode", SqlDbType.NVarChar, 50).Value = adjustCode;
                    cmdHeader.Parameters.Add("@AdjustDate", SqlDbType.DateTime).Value = dto.AdjustDate;
                    cmdHeader.Parameters.Add("@FACID", SqlDbType.Int).Value = (object?)dto.FACID ?? DBNull.Value;
                    cmdHeader.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = (object?)dto.Note ?? DBNull.Value;
                    cmdHeader.Parameters.Add("@CreateBy", SqlDbType.UniqueIdentifier).Value = (object?)currentUser?.Id ?? DBNull.Value;
                    adjustId = (int)await cmdHeader.ExecuteScalarAsync();
                }

                // 2. Insert attachments
                if (dto.AttachmentUrls != null && dto.AttachmentUrls.Any())
                {
                    foreach (var url in dto.AttachmentUrls)
                    {
                        var ext = Path.GetExtension(url);
                        var name = Path.GetFileName(url);
                        const string sqlAttach = @"
                            INSERT INTO dbo.Tbl_Attachments (Id, FilePath, FileExtend, FileName, CreatedTime, ReferenceType, ReferenceID)
                            VALUES (NEWID(), @FilePath, @FileExtend, @FileName, GETDATE(), 'Adjust', @ReferenceID)";
                        
                        await using (var cmdAttach = new SqlCommand(sqlAttach, con, (SqlTransaction)tran))
                        {
                            cmdAttach.Parameters.Add("@FilePath", SqlDbType.NVarChar, 500).Value = url;
                            cmdAttach.Parameters.Add("@FileExtend", SqlDbType.NVarChar, 50).Value = ext ?? (object)DBNull.Value;
                            cmdAttach.Parameters.Add("@FileName", SqlDbType.NVarChar, 255).Value = name;
                            cmdAttach.Parameters.Add("@ReferenceID", SqlDbType.NVarChar, 50).Value = adjustId.ToString();
                            await cmdAttach.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 3. Process each detail line
                foreach (var line in dto.Lines)
                {
                    if (line.Quantity <= 0)
                        throw new ArgumentException($"Số lượng cho phụ tùng SPID {line.SPID} phải lớn hơn 0.");

                    // Verify spare part exists and check its coded status
                    const string sqlCheckPart = "SELECT IsCoded, PartName, PartCode, Price FROM dbo.Tbl_SparePart WHERE SPID = @SPID";
                    bool isCoded = false;
                    string partName = "";
                    string partCode = "";
                    await using (var cmdCheck = new SqlCommand(sqlCheckPart, con, (SqlTransaction)tran))
                    {
                        cmdCheck.Parameters.Add("@SPID", SqlDbType.Int).Value = line.SPID;
                        await using var reader = await cmdCheck.ExecuteReaderAsync();
                        if (!await reader.ReadAsync())
                            throw new KeyNotFoundException($"Không tìm thấy phụ tùng SPID = {line.SPID}.");
                        isCoded = Convert.ToBoolean(reader["IsCoded"]);
                        partName = reader["PartName"].ToString() ?? "";
                        partCode = reader["PartCode"].ToString() ?? "";
                    }

                    if (isCoded)
                    {
                        if (line.Quantity != 1)
                            throw new ArgumentException($"Phụ tùng quản lý theo Serial/Code '{partName}' phải có số lượng bằng 1.");
                        if (string.IsNullOrWhiteSpace(line.SerialCode))
                            throw new ArgumentException($"Phụ tùng quản lý theo Serial/Code '{partName}' yêu cầu phải nhập mã Serial.");
                    }

                    // Get current inventory (BeforeQty)
                    const string sqlGetInv = "SELECT Inventory FROM dbo.Tbl_SparePart WHERE SPID = @SPID";
                    int beforeQty = 0;
                    await using (var cmdInv = new SqlCommand(sqlGetInv, con, (SqlTransaction)tran))
                    {
                        cmdInv.Parameters.Add("@SPID", SqlDbType.Int).Value = line.SPID;
                        var resInv = await cmdInv.ExecuteScalarAsync();
                        beforeQty = resInv == null ? 0 : Convert.ToInt32(resInv);
                    }

                    int? itemID = null;

                    if (line.Type == "IN")
                    {
                        if (isCoded)
                        {
                            // Verify SerialCode does not already exist as Available
                            const string sqlSerialCheck = "SELECT COUNT(1) FROM dbo.Tbl_SparePartItem WHERE SerialCode = @SerialCode AND Status = 'Available'";
                            await using (var cmdSerial = new SqlCommand(sqlSerialCheck, con, (SqlTransaction)tran))
                            {
                                cmdSerial.Parameters.Add("@SerialCode", SqlDbType.NVarChar, 100).Value = line.SerialCode;
                                int exists = Convert.ToInt32(await cmdSerial.ExecuteScalarAsync());
                                if (exists > 0)
                                    throw new InvalidOperationException($"Mã Serial '{line.SerialCode}' của phụ tùng '{partName}' đã tồn tại trong kho và đang có sẵn.");
                            }
                        }

                        itemID = await CreateSparePartItemRecordInternalAsync(
                            con,
                            (SqlTransaction)tran,
                            line.SPID,
                            null,
                            null,
                            line.SerialCode,
                            line.Quantity,
                            dto.AdjustDate,
                            currentUser?.Id);

                        // Insert transaction log
                        var adjInId = await GetMovementTypeIdByNameInternalAsync(con, MovementTypeConstants.ManualAdjustIn, tran);
                        const string sqlTx = @"
                            INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, RefCode, Note, CreateBy, CreateDate, MovementType, MovementTypeID)
                            VALUES (@SPID, 'IN', @Quantity, @Date, @RefCode, @Note, @CreateBy, GETDATE(), 'balance', @MovementTypeID)";
                        
                        await using (var cmdTx = new SqlCommand(sqlTx, con, (SqlTransaction)tran))
                        {
                            cmdTx.Parameters.Add("@SPID", SqlDbType.Int).Value = line.SPID;
                            cmdTx.Parameters.Add("@Quantity", SqlDbType.Int).Value = line.Quantity;
                            cmdTx.Parameters.Add("@Date", SqlDbType.DateTime).Value = dto.AdjustDate;
                            cmdTx.Parameters.Add("@RefCode", SqlDbType.NVarChar, 30).Value = adjustCode;
                            cmdTx.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = $"Điều chỉnh tăng (Serial: {line.SerialCode ?? "N/A"})";
                            cmdTx.Parameters.Add("@CreateBy", SqlDbType.UniqueIdentifier).Value = (object?)currentUser?.Id ?? DBNull.Value;
                            cmdTx.Parameters.Add("@MovementTypeID", SqlDbType.Int).Value = (object?)adjInId ?? DBNull.Value;
                            await cmdTx.ExecuteNonQueryAsync();
                        }
                    }
                    else if (line.Type == "OUT")
                    {
                        if (isCoded)
                        {
                            // Verify SerialCode exists as Available
                            const string sqlSerialCheck = "SELECT ItemID FROM dbo.Tbl_SparePartItem WHERE SPID = @SPID AND SerialCode = @SerialCode AND Status = 'Available'";
                            await using (var cmdSerial = new SqlCommand(sqlSerialCheck, con, (SqlTransaction)tran))
                            {
                                cmdSerial.Parameters.Add("@SPID", SqlDbType.Int).Value = line.SPID;
                                cmdSerial.Parameters.Add("@SerialCode", SqlDbType.NVarChar, 100).Value = line.SerialCode;
                                var resItem = await cmdSerial.ExecuteScalarAsync();
                                if (resItem == null)
                                    throw new InvalidOperationException($"Mã Serial '{line.SerialCode}' của phụ tùng '{partName}' không tồn tại hoặc không còn sẵn trong kho.");
                                itemID = Convert.ToInt32(resItem);
                            }

                            // Update item status to Adjusted and RemainingQuantity = 0
                            const string sqlUpdateItem = "UPDATE dbo.Tbl_SparePartItem SET RemainingQuantity = 0, Status = 'Adjusted', UpdateAt = GETDATE() WHERE ItemID = @ItemID";
                            await using (var cmdUpdate = new SqlCommand(sqlUpdateItem, con, (SqlTransaction)tran))
                            {
                                cmdUpdate.Parameters.Add("@ItemID", SqlDbType.Int).Value = itemID;
                                await cmdUpdate.ExecuteNonQueryAsync();
                            }

                            // Insert transaction log
                            var adjOutId = await GetMovementTypeIdByNameInternalAsync(con, MovementTypeConstants.ManualAdjustOut, tran);
                            const string sqlTx = @"
                                INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, RefCode, Note, CreateBy, CreateDate, MovementType, MovementTypeID)
                                VALUES (@SPID, 'OUT', 1, @Date, @RefCode, @Note, @CreateBy, GETDATE(), 'balance', @MovementTypeID)";
                            
                            await using (var cmdTx = new SqlCommand(sqlTx, con, (SqlTransaction)tran))
                            {
                                cmdTx.Parameters.Add("@SPID", SqlDbType.Int).Value = line.SPID;
                                cmdTx.Parameters.Add("@Date", SqlDbType.DateTime).Value = dto.AdjustDate;
                                cmdTx.Parameters.Add("@RefCode", SqlDbType.NVarChar, 30).Value = adjustCode;
                                cmdTx.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = $"Điều chỉnh giảm (Serial: {line.SerialCode})";
                                cmdTx.Parameters.Add("@CreateBy", SqlDbType.UniqueIdentifier).Value = (object?)currentUser?.Id ?? DBNull.Value;
                                cmdTx.Parameters.Add("@MovementTypeID", SqlDbType.Int).Value = (object?)adjOutId ?? DBNull.Value;
                                await cmdTx.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // Deduct Non-Coded FIFO
                            var deductions = await DeductSparePartItemFIFOAsync(con, (SqlTransaction)tran, line.SPID, line.Quantity, "Adjusted");
                            
                            // Insert transaction log for the total OUT deduction
                            var adjOutId = await GetMovementTypeIdByNameInternalAsync(con, MovementTypeConstants.ManualAdjustOut, tran);
                            const string sqlTx = @"
                                INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, RefCode, Note, CreateBy, CreateDate, MovementType, MovementTypeID)
                                VALUES (@SPID, 'OUT', @Quantity, @Date, @RefCode, @Note, @CreateBy, GETDATE(), 'balance', @MovementTypeID)";
                            
                            await using (var cmdTx = new SqlCommand(sqlTx, con, (SqlTransaction)tran))
                            {
                                cmdTx.Parameters.Add("@SPID", SqlDbType.Int).Value = line.SPID;
                                cmdTx.Parameters.Add("@Quantity", SqlDbType.Int).Value = line.Quantity;
                                cmdTx.Parameters.Add("@Date", SqlDbType.DateTime).Value = dto.AdjustDate;
                                cmdTx.Parameters.Add("@RefCode", SqlDbType.NVarChar, 30).Value = adjustCode;
                                cmdTx.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = "Điều chỉnh giảm tồn kho";
                                cmdTx.Parameters.Add("@CreateBy", SqlDbType.UniqueIdentifier).Value = (object?)currentUser?.Id ?? DBNull.Value;
                                cmdTx.Parameters.Add("@MovementTypeID", SqlDbType.Int).Value = (object?)adjOutId ?? DBNull.Value;
                                await cmdTx.ExecuteNonQueryAsync();
                            }

                            // If multi-deductions occurred, we set itemID to the first deducted item or null
                            if (deductions.Any())
                            {
                                itemID = deductions.First().ItemID;
                            }
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Loại điều chỉnh chỉ chấp nhận IN hoặc OUT.");
                    }

                    // Recalculate inventory
                    await SyncInventoryStockAsync(con, (SqlTransaction)tran, line.SPID);

                    // Fetch after inventory (AfterQty)
                    int afterQty = 0;
                    await using (var cmdInv = new SqlCommand(sqlGetInv, con, (SqlTransaction)tran))
                    {
                        cmdInv.Parameters.Add("@SPID", SqlDbType.Int).Value = line.SPID;
                        var resInv = await cmdInv.ExecuteScalarAsync();
                        afterQty = resInv == null ? 0 : Convert.ToInt32(resInv);
                    }

                    // Insert detail line
                    const string sqlDetail = @"
                        INSERT INTO dbo.Tbl_AdjustOrderDetail (AdjustID, SPID, Type, HasCode, SerialCode, Quantity, BeforeQty, AfterQty, ItemID)
                        VALUES (@AdjustID, @SPID, @Type, @HasCode, @SerialCode, @Quantity, @BeforeQty, @AfterQty, @ItemID)";
                    
                    await using (var cmdDetail = new SqlCommand(sqlDetail, con, (SqlTransaction)tran))
                    {
                        cmdDetail.Parameters.Add("@AdjustID", SqlDbType.Int).Value = adjustId;
                        cmdDetail.Parameters.Add("@SPID", SqlDbType.Int).Value = line.SPID;
                        cmdDetail.Parameters.Add("@Type", SqlDbType.NVarChar, 20).Value = line.Type;
                        cmdDetail.Parameters.Add("@HasCode", SqlDbType.Bit).Value = isCoded;
                        cmdDetail.Parameters.Add("@SerialCode", SqlDbType.NVarChar, 100).Value = (object?)line.SerialCode ?? DBNull.Value;
                        cmdDetail.Parameters.Add("@Quantity", SqlDbType.Int).Value = line.Quantity;
                        cmdDetail.Parameters.Add("@BeforeQty", SqlDbType.Int).Value = beforeQty;
                        cmdDetail.Parameters.Add("@AfterQty", SqlDbType.Int).Value = afterQty;
                        cmdDetail.Parameters.Add("@ItemID", SqlDbType.Int).Value = (object?)itemID ?? DBNull.Value;
                        await cmdDetail.ExecuteNonQueryAsync();
                    }
                }

                await tran.CommitAsync();
                
                var createdOrder = await GetAdjustOrderByIdAsync(adjustId);
                return createdOrder;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }
    }
}
