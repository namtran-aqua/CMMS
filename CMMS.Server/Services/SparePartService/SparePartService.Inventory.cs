using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CMMS.Server.Services.SparePartService
{
    public partial class SparePartService
    {
        public async Task<bool> AdjustStockAsync(AdjustStockRequestDto request, UserDto currentUser)
        {
            if (request.Type != "IN" && request.Type != "OUT")
                throw new ArgumentException("Loại giao dịch không hợp lệ. Chỉ chấp nhận IN hoặc OUT.");
            if (request.Qty <= 0)
                throw new ArgumentException("Số lượng phải lớn hơn 0.");

            var connStr = _config.GetConnectionString("DefaultConnection");

            await using var con = new SqlConnection(connStr);
            await con.OpenAsync();
            await using var tran = await con.BeginTransactionAsync();

            try
            {
                int currentStock = await LockAndGetStockAsync(con, (SqlTransaction)tran, request.SPID);

                var delta = request.Type == "IN" ? request.Qty : -request.Qty;
                var newStock = currentStock + delta;

                if (newStock < 0)
                    throw new InvalidOperationException($"Không đủ tồn kho. Hiện chỉ còn {currentStock}, không thể xuất {request.Qty}.");

                await UpdateStockAsync(con, (SqlTransaction)tran, request.SPID, newStock);

                var movementTypeName = request.Type == "IN" ? MovementTypeConstants.ManualAdjustIn : MovementTypeConstants.ManualAdjustOut;
                var movementTypeId = await GetMovementTypeIdByNameInternalAsync(con, movementTypeName, tran);

                const string sqlInsertTx = @"
                    INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, RefCode, Note, CreateBy, CreateDate, MovementType, MovementTypeID)
                    VALUES (@SPID, @Type, @Qty, @Date, @RefCode, @Note, @CreateBy, @CreateDate, @MovementType, @MovementTypeID)";
                await using (var cmdTx = new SqlCommand(sqlInsertTx, con, (SqlTransaction)tran))
                {
                    cmdTx.Parameters.Add("@SPID", SqlDbType.Int).Value = request.SPID;
                    cmdTx.Parameters.Add("@Type", SqlDbType.NVarChar, 20).Value = request.Type;
                    cmdTx.Parameters.Add("@Qty", SqlDbType.Int).Value = request.Qty;
                    cmdTx.Parameters.Add("@Date", SqlDbType.DateTime).Value = DateTime.Now;
                    cmdTx.Parameters.Add("@RefCode", SqlDbType.NVarChar, 30).Value = (object?)request.RefCode ?? DBNull.Value;
                    cmdTx.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = (object?)request.Note ?? DBNull.Value;
                    cmdTx.Parameters.Add("@CreateBy", SqlDbType.UniqueIdentifier).Value = (object?)currentUser?.Id ?? DBNull.Value;
                    cmdTx.Parameters.Add("@CreateDate", SqlDbType.DateTime).Value = DateTime.Now;
                    cmdTx.Parameters.Add("@MovementType", SqlDbType.NVarChar, 50).Value = "ADJUST";
                    cmdTx.Parameters.Add("@MovementTypeID", SqlDbType.Int).Value = (object?)movementTypeId ?? DBNull.Value;
                    await cmdTx.ExecuteNonQueryAsync();
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

        public async Task<string> ExportForMaintenanceAsync(MaintenanceExportRequestDto request, UserDto currentUser)
        {
            if (string.IsNullOrWhiteSpace(request.Equipment))
                throw new ArgumentException("Vui lòng nhập tên thiết bị / công việc bảo trì.");
            if (request.Lines == null || !request.Lines.Any())
                throw new ArgumentException("Vui lòng chọn ít nhất một phụ tùng.");

            var connStr = _config.GetConnectionString("DefaultConnection");
            var refCode = "BT-" + DateTime.Now.ToString("yyMMddHHmmss");

            await using var con = new SqlConnection(connStr);
            await con.OpenAsync();
            await using var tran = await con.BeginTransactionAsync();

            try
            {
                foreach (var line in request.Lines)
                {
                    if (line.Qty <= 0) continue;

                    int currentStock = await LockAndGetStockAsync(con, (SqlTransaction)tran, line.SPID);
                    var newStock = currentStock - line.Qty;

                    if (newStock < 0)
                        throw new InvalidOperationException($"Phụ tùng SPID = {line.SPID} không đủ tồn kho (còn {currentStock}, cần {line.Qty}).");

                    await UpdateStockAsync(con, (SqlTransaction)tran, line.SPID, newStock);

                    var noteText = string.IsNullOrWhiteSpace(request.RequestedBy)
                        ? request.Note
                        : $"Người yêu cầu: {request.RequestedBy}" + (string.IsNullOrWhiteSpace(request.Note) ? "" : $" — {request.Note}");

                    var maintTypeId = await GetMovementTypeIdByNameInternalAsync(con, MovementTypeConstants.Maintenance, tran);

                    const string sqlInsertTx = @"
                        INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, RefCode, EQID, Equipment, Note, CreateBy, MovementType, MovementTypeID)
                        VALUES (@SPID, 'OUT', @Qty, @Date, @RefCode, @EQID, @Equipment, @Note, @CreateBy, @MovementType, @MovementTypeID)";
                    await using (var cmdTx = new SqlCommand(sqlInsertTx, con, (SqlTransaction)tran))
                    {
                        cmdTx.Parameters.Add("@SPID", SqlDbType.Int).Value = line.SPID;
                        cmdTx.Parameters.Add("@Qty", SqlDbType.Int).Value = line.Qty;
                        cmdTx.Parameters.Add("@Date", SqlDbType.DateTime).Value = DateTime.Now;
                        cmdTx.Parameters.Add("@RefCode", SqlDbType.NVarChar, 30).Value = refCode;
                        cmdTx.Parameters.Add("@EQID", SqlDbType.Int).Value = (object?)request.EQID ?? DBNull.Value;
                        cmdTx.Parameters.Add("@Equipment", SqlDbType.NVarChar, 150).Value = request.Equipment;
                        cmdTx.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = (object?)noteText ?? DBNull.Value;
                        cmdTx.Parameters.Add("@CreateBy", SqlDbType.UniqueIdentifier).Value = (object?)currentUser?.Id ?? DBNull.Value;
                        cmdTx.Parameters.Add("@MovementType", SqlDbType.NVarChar, 50).Value = "MAINTENANCE";
                        cmdTx.Parameters.Add("@MovementTypeID", SqlDbType.Int).Value = (object?)maintTypeId ?? DBNull.Value;
                        await cmdTx.ExecuteNonQueryAsync();
                    }
                }

                await tran.CommitAsync();
                return refCode;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        private static async Task<int> LockAndGetStockAsync(SqlConnection con, SqlTransaction tran, int spid)
        {
            const string sqlLock = "SELECT Inventory FROM dbo.Tbl_SparePart WITH (UPDLOCK, ROWLOCK) WHERE SPID = @SPID";
            await using var cmd = new SqlCommand(sqlLock, con, tran);
            cmd.Parameters.Add("@SPID", SqlDbType.Int).Value = spid;
            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
                throw new KeyNotFoundException($"Không tìm thấy phụ tùng SPID = {spid}.");
            return Convert.ToInt32(result);
        }

        private static async Task UpdateStockAsync(SqlConnection con, SqlTransaction tran, int spid, int newStock)
        {
            const string sql = "UPDATE dbo.Tbl_SparePart SET Inventory = @Inventory, UpdateDate = @UpdateDate WHERE SPID = @SPID";
            await using var cmd = new SqlCommand(sql, con, tran);
            cmd.Parameters.Add("@Inventory", SqlDbType.Int).Value = newStock;
            cmd.Parameters.Add("@UpdateDate", SqlDbType.DateTime).Value = DateTime.Now;
            cmd.Parameters.Add("@SPID", SqlDbType.Int).Value = spid;
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
