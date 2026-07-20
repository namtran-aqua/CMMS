using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace CMMS.Server.Services.SparePartService
{
    public partial class SparePartService
    {
        private async Task SyncInventoryStockAsync(SqlConnection con, SqlTransaction tran, int spid)
        {
            const string sql = @"
                UPDATE dbo.Tbl_SparePart 
                SET Inventory = ISNULL((SELECT SUM(RemainingQuantity) FROM dbo.Tbl_SparePartItem WHERE SPID = @SPID AND Status = 'Available'), 0),
                    UpdateDate = GETDATE()
                WHERE SPID = @SPID";
            await using var cmd = new SqlCommand(sql, con, tran);
            cmd.Parameters.Add("@SPID", SqlDbType.Int).Value = spid;
            await cmd.ExecuteNonQueryAsync();
        }

        internal async Task<int> CreateSparePartItemRecordInternalAsync(
            IDbConnection con, 
            IDbTransaction tran, 
            int spid, 
            int? importId, 
            int? importDetailId, 
            string? serialCode, 
            int qty, 
            DateTime importDate, 
            Guid? createBy)
        {
            // 1. Get FACID and DeptID from Spare Part
            const string sqlPart = "SELECT FACID, DeptID FROM dbo.Tbl_SparePart WHERE SPID = @SPID";
            var partInfo = await con.QueryFirstOrDefaultAsync<dynamic>(sqlPart, new { SPID = spid }, tran);
            int? facId = partInfo?.FACID;
            int? deptId = partInfo?.DeptID;

            // 2. Insert SparePartItem
            const string sqlInsert = @"
                INSERT INTO dbo.Tbl_SparePartItem (SPID, ImportID, ImportDetailID, HasCode, SerialCode, Quantity, RemainingQuantity, ImportDate, Status, CreateBy, CreateAt, FACID, DeptID)
                VALUES (@SPID, @ImportID, @ImportDetailID, @HasCode, @SerialCode, @Quantity, @RemainingQuantity, @ImportDate, 'Available', @CreateBy, @CreateAt, @FACID, @DeptID);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            
            return await con.ExecuteScalarAsync<int>(sqlInsert, new {
                SPID = spid,
                ImportID = importId,
                ImportDetailID = importDetailId,
                HasCode = !string.IsNullOrWhiteSpace(serialCode),
                SerialCode = serialCode,
                Quantity = qty,
                RemainingQuantity = qty,
                ImportDate = importDate,
                CreateBy = createBy,
                CreateAt = DateTime.Now,
                FACID = facId,
                DeptID = deptId
            }, tran);
        }

        private async Task<List<(int ItemID, int DeductedQty)>> DeductSparePartItemFIFOAsync(SqlConnection con, SqlTransaction tran, int spid, int qty, string statusIfConsumed)
        {
            const string sqlSelect = @"
                SELECT ItemID, RemainingQuantity 
                FROM dbo.Tbl_SparePartItem 
                WHERE SPID = @SPID AND Status = 'Available'
                ORDER BY ImportDate ASC, ItemID ASC";
            
            var items = new List<(int ItemID, int RemainingQuantity)>();
            await using (var cmdSelect = new SqlCommand(sqlSelect, con, tran))
            {
                cmdSelect.Parameters.Add("@SPID", SqlDbType.Int).Value = spid;
                await using var reader = await cmdSelect.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(((int)reader["ItemID"], (int)reader["RemainingQuantity"]));
                }
            }

            var remainingToDeduct = qty;
            var deductions = new List<(int ItemID, int DeductedQty)>();

            foreach (var item in items)
            {
                if (remainingToDeduct <= 0) break;

                var deductAmount = Math.Min(item.RemainingQuantity, remainingToDeduct);
                var newRemaining = item.RemainingQuantity - deductAmount;
                var newStatus = newRemaining == 0 ? statusIfConsumed : "Available";

                const string sqlUpdate = @"
                    UPDATE dbo.Tbl_SparePartItem 
                    SET RemainingQuantity = @RemainingQty, 
                        Status = @Status, 
                        UpdateAt = GETDATE() 
                    WHERE ItemID = @ItemID";
                
                await using (var cmdUpdate = new SqlCommand(sqlUpdate, con, tran))
                {
                    cmdUpdate.Parameters.Add("@RemainingQty", SqlDbType.Int).Value = newRemaining;
                    cmdUpdate.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = newStatus;
                    cmdUpdate.Parameters.Add("@ItemID", SqlDbType.Int).Value = item.ItemID;
                    await cmdUpdate.ExecuteNonQueryAsync();
                }

                deductions.Add((item.ItemID, deductAmount));
                remainingToDeduct -= deductAmount;
            }

            if (remainingToDeduct > 0)
            {
                throw new InvalidOperationException($"Không đủ tồn kho khả dụng để thực hiện xuất. Còn thiếu {remainingToDeduct}.");
            }

            return deductions;
        }
    }
}
