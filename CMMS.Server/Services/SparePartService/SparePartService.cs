using CMMS.Data.Connection;
using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CMMS.Server.Services.SparePartService
{
    public class SparePartService : ISparePartService
    {
        private readonly IConfiguration _config;
        private readonly ISqlConnectionFactory _connectionFactory;

        public SparePartService(IConfiguration config, ISqlConnectionFactory connectionFactory)
        {
            _config = config;
            _connectionFactory = connectionFactory;
        }

        public async Task<List<SparePartDto>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    p.SPID, p.PartCode, p.PartName, p.CategoryID, c.CategoryName,
                    p.Unit, p.Price, p.Inventory, p.MinStock, p.LocID, l.LocName AS Location,
                    p.SupplierID, s.SupplierName, p.CreateDate, p.UpdateDate,
                    COALESCE(l.FACID, d.FACID) AS FACID
                FROM dbo.Tbl_SparePart p
                LEFT JOIN dbo.Tbl_SparePartCategories c ON c.CategoryID = p.CategoryID
                LEFT JOIN dbo.Tbl_SparePartSuppliers s ON s.SupplierID = p.SupplierID
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                LEFT JOIN dbo.vw_FactoryDepartment d ON d.DeptID = p.DeptID
                ORDER BY p.PartName";

            var parts = await connection.QueryAsync<SparePartDto>(sql);
            return parts.ToList();
        }

        public async Task<List<SparePartCategoryDto>> GetCategoriesAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT CategoryID, CategoryName FROM dbo.Tbl_SparePartCategories ORDER BY CategoryName";
            return (await connection.QueryAsync<SparePartCategoryDto>(sql)).ToList();
        }

        public async Task<List<SparePartSupplierDto>> GetSuppliersAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT SupplierID, SupplierName, Phone, Email FROM dbo.Tbl_SparePartSuppliers ORDER BY SupplierName";
            return (await connection.QueryAsync<SparePartSupplierDto>(sql)).ToList();
        }

        public async Task<List<SparePartTransactionDto>> GetTransactionHistoryAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    t.TransID, t.SPID, p.PartCode, p.PartName,
                    t.Type, t.Quantity, t.Date, t.EQID, t.Note, t.MTID, t.CreateBy, t.CreateDate, u.WorkDayId AS CreateUser,
                    COALESCE(l.FACID, d.FACID) AS FACID
                FROM dbo.Tbl_Transactions t
                LEFT JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                LEFT JOIN dbo.vw_FactoryDepartment d ON d.DeptID = p.DeptID
                LEFT JOIN dbo.Tbl_User u ON t.CreateBy = u.Id
                LEFT JOIN dbo.Tbl_MaintenanceRecord m ON t.MTID = m.MTID
                ORDER BY t.TransID DESC";

            return (await connection.QueryAsync<SparePartTransactionDto>(sql)).ToList();
        }

        public async Task<SparePartDto> CreateAsync(SparePartDto dto, UserDto currentUser)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            const string sqlCheckCode = "SELECT COUNT(1) FROM dbo.Tbl_SparePart WHERE PartCode = @PartCode";
            const string sqlInsert = @"
                INSERT INTO dbo.Tbl_SparePart
                    (PartCode, PartName, CategoryID, Unit, Price, Inventory, MinStock, LocID, DeptID, SupplierID, Note, CreateDate, UpdateDate, CreateBy)
                VALUES
                    (@PartCode, @PartName, @CategoryID, @Unit, @Price, @Inventory, @MinStock, @LocID, @DeptID, @SupplierID, @Note, @CreateDate, @UpdateDate, @CreateBy);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var con = new SqlConnection(connStr);
            await con.OpenAsync();

            await using (var checkCmd = new SqlCommand(sqlCheckCode, con))
            {
                checkCmd.Parameters.Add("@PartCode", SqlDbType.NVarChar, 30).Value = dto.PartCode;
                var count = (int)await checkCmd.ExecuteScalarAsync();
                if (count > 0)
                    throw new InvalidOperationException($"Mã phụ tùng '{dto.PartCode}' đã tồn tại.");
            }

            int newId;
            await using (var cmd = new SqlCommand(sqlInsert, con))
            {
                cmd.Parameters.Add("@PartCode", SqlDbType.NVarChar, 30).Value = dto.PartCode;
                cmd.Parameters.Add("@PartName", SqlDbType.NVarChar, 200).Value = dto.PartName;
                cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = (object?)dto.CategoryID ?? DBNull.Value;
                cmd.Parameters.Add("@Unit", SqlDbType.NVarChar, 20).Value = dto.Unit;

                var priceParam = cmd.Parameters.Add("@Price", SqlDbType.Decimal);
                priceParam.Precision = 18;
                priceParam.Scale = 2;
                priceParam.Value = (object?)dto.Price ?? 0m;

                cmd.Parameters.Add("@Inventory", SqlDbType.Int).Value = dto.Inventory;
                cmd.Parameters.Add("@MinStock", SqlDbType.Int).Value = dto.MinStock;
                cmd.Parameters.Add("@LocID", SqlDbType.Int).Value = (object?)dto.LocID ?? DBNull.Value;
                cmd.Parameters.Add("@DeptID", SqlDbType.Int).Value = (object?)dto.DeptID ?? DBNull.Value;
                cmd.Parameters.Add("@SupplierID", SqlDbType.Int).Value = (object?)dto.SupplierID ?? DBNull.Value;
                cmd.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = (object?)dto.Note ?? DBNull.Value;
                cmd.Parameters.Add("@CreateDate", SqlDbType.Date).Value = DateTime.Today;
                cmd.Parameters.Add("@UpdateDate", SqlDbType.Date).Value = DateTime.Today;
                cmd.Parameters.Add("@CreateBy", SqlDbType.UniqueIdentifier).Value = (object?)currentUser?.Id ?? DBNull.Value;

                var result = await cmd.ExecuteScalarAsync();
                newId = Convert.ToInt32(result);
            }

            dto.SPID = newId;
            return dto;
        }

        public async Task<bool> UpdateAsync(SparePartDto dto, UserDto currentUser)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            const string sqlCheckCode = "SELECT COUNT(1) FROM dbo.Tbl_SparePart WHERE PartCode = @PartCode AND SPID <> @SPID";
            const string sqlUpdate = @"
                UPDATE dbo.Tbl_SparePart
                SET PartCode = @PartCode, PartName = @PartName, CategoryID = @CategoryID, Unit = @Unit,
                    Price = @Price, MinStock = @MinStock, LocID = @LocID,
                    SupplierID = @SupplierID, Note = @Note, UpdateDate = @UpdateDate, UpdateBy = @UpdateBy
                WHERE SPID = @SPID";

            await using var con = new SqlConnection(connStr);
            await con.OpenAsync();

            await using (var checkCmd = new SqlCommand(sqlCheckCode, con))
            {
                checkCmd.Parameters.Add("@PartCode", SqlDbType.NVarChar, 30).Value = dto.PartCode;
                checkCmd.Parameters.Add("@SPID", SqlDbType.Int).Value = dto.SPID;
                var count = (int)await checkCmd.ExecuteScalarAsync();
                if (count > 0)
                    throw new InvalidOperationException($"Mã phụ tùng '{dto.PartCode}' đã được dùng cho phụ tùng khác.");
            }

            await using var cmd = new SqlCommand(sqlUpdate, con);
            cmd.Parameters.Add("@SPID", SqlDbType.Int).Value = dto.SPID;
            cmd.Parameters.Add("@PartCode", SqlDbType.NVarChar, 30).Value = dto.PartCode;
            cmd.Parameters.Add("@PartName", SqlDbType.NVarChar, 200).Value = dto.PartName;
            cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = (object?)dto.CategoryID ?? DBNull.Value;
            cmd.Parameters.Add("@Unit", SqlDbType.NVarChar, 20).Value = dto.Unit;

            var priceParam = cmd.Parameters.Add("@Price", SqlDbType.Decimal);
            priceParam.Precision = 18;
            priceParam.Scale = 2;
            priceParam.Value = (object?)dto.Price ?? 0m;

            cmd.Parameters.Add("@MinStock", SqlDbType.Int).Value = dto.MinStock;
            cmd.Parameters.Add("@LocID", SqlDbType.Int).Value = (object?)dto.LocID ?? DBNull.Value;
            cmd.Parameters.Add("@SupplierID", SqlDbType.Int).Value = (object?)dto.SupplierID ?? DBNull.Value;
            cmd.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = (object?)dto.Note ?? DBNull.Value;
            cmd.Parameters.Add("@UpdateDate", SqlDbType.Date).Value = DateTime.Today;
            cmd.Parameters.Add("@UpdateBy", SqlDbType.UniqueIdentifier).Value = (object?)currentUser?.Id ?? DBNull.Value;

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int spid, UserDto currentUser)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "DELETE FROM dbo.Tbl_SparePart WHERE SPID = @SPID";
            var rows = await connection.ExecuteAsync(sql, new { SPID = spid });
            return rows > 0;
        }

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

                //var txType = request.Type == "OUT" ? "OUTBOUND" : request.Type;

                const string sqlInsertTx = @"
                    INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, RefCode, Note, CreateBy, CreateDate)
                    VALUES (@SPID, @Type, @Qty, @Date, @RefCode, @Note, @CreateBy, @CreateDate)";
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

                    const string sqlInsertTx = @"
                        INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, RefCode, EQID, Equipment, Note, CreateBy)
                        VALUES (@SPID, 'MAINTENANCE', @Qty, @Date, @RefCode, @EQID, @Equipment, @Note, @CreateBy)";
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

        // Khóa dòng phụ tùng để tránh 2 người cùng xuất kho một lúc gây âm tồn kho
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
        public async Task<SparePartCategoryDto> CreateCategory(SparePartCategoryDto dto, UserDto currentUser)
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName))
                throw new ArgumentException("CategoryName không được để trống.");

            var connStr = _config.GetConnectionString("DefaultConnection");

            const string sqlCheckExists =
                "SELECT COUNT(1) FROM dbo.Tbl_SparePartCategories WHERE CategoryName = @CategoryName";

            const string sqlInsert = @"
        INSERT INTO dbo.Tbl_SparePartCategories (CategoryName, CreateDate, CreateBy)
        VALUES (@CategoryName, @CreateDate, @CreateBy);
        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var con = new SqlConnection(connStr);
            await con.OpenAsync();
            await using var tran = con.BeginTransaction();

            try
            {
                await using (var checkCmd = new SqlCommand(sqlCheckExists, con, tran))
                {
                    checkCmd.Parameters.Add("@CategoryName", SqlDbType.NVarChar, 50).Value = dto.CategoryName;
                    var exists = (int)await checkCmd.ExecuteScalarAsync();
                    if (exists > 0)
                        throw new InvalidOperationException($"Category '{dto.CategoryName}' đã tồn tại.");
                }

                int newId;
                await using (var cmd = new SqlCommand(sqlInsert, con, tran))
                {
                    cmd.Parameters.Add("@CategoryName", SqlDbType.NVarChar, 50).Value = dto.CategoryName;
                    cmd.Parameters.Add("@CreateDate", SqlDbType.DateTime).Value = DateTime.Now;
                    cmd.Parameters.Add("@CreateBy", SqlDbType.UniqueIdentifier, 16).Value = (object?)currentUser?.Id ?? DBNull.Value;
                    var result = await cmd.ExecuteScalarAsync();
                    newId = Convert.ToInt32(result);
                }

                await tran.CommitAsync();

                dto.CategoryID = newId;
                return dto;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> DeleteCategory(int categoryid, UserDto currentUser)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "DELETE FROM dbo.Tbl_SparePartCategories WHERE CategoryID = @CategoryID";
            var rows = await connection.ExecuteAsync(sql, new { CategoryID = categoryid });
            return rows > 0;
        }
        public async Task<SparePartSupplierDto> CreateSupplier(SparePartSupplierDto dto, UserDto currentUser)
        {
            if (string.IsNullOrWhiteSpace(dto.SupplierName))
                throw new ArgumentException("SupplierName không được để trống.");

            var connStr = _config.GetConnectionString("DefaultConnection");

            const string sqlCheckExists =
                "SELECT COUNT(1) FROM dbo.Tbl_SparePartSuppliers WHERE SupplierName = @SupplierName";

            const string sqlInsert = @"
        INSERT INTO dbo.Tbl_SparePartSuppliers (SupplierName, Phone, Email, CreateDate, CreateBy)
        VALUES (@SupplierName, @Phone, @Email, @CreateDate, @CreateBy);
        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var con = new SqlConnection(connStr);
            await con.OpenAsync();
            await using var tran = con.BeginTransaction();

            try
            {
                await using (var checkCmd = new SqlCommand(sqlCheckExists, con, tran))
                {
                    checkCmd.Parameters.Add("@SupplierName", SqlDbType.NVarChar, 50).Value = dto.SupplierName;
                    var exists = (int)await checkCmd.ExecuteScalarAsync();
                    if (exists > 0)
                        throw new InvalidOperationException($"Supplier '{dto.SupplierName}' đã tồn tại.");
                }

                int newId;
                await using (var cmd = new SqlCommand(sqlInsert, con, tran))
                {
                    cmd.Parameters.Add("@SupplierName", SqlDbType.NVarChar, 50).Value = dto.SupplierName;
                    cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = (object?)dto.Phone ?? DBNull.Value;
                    cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 50).Value = (object?)dto.Email ?? DBNull.Value;
                    cmd.Parameters.Add("@CreateDate", SqlDbType.DateTime).Value = DateTime.Now;
                    cmd.Parameters.Add("@CreateBy", SqlDbType.UniqueIdentifier, 16).Value = (object?)currentUser?.Id ?? DBNull.Value;
                    var result = await cmd.ExecuteScalarAsync();
                    newId = Convert.ToInt32(result);
                }

                await tran.CommitAsync();

                dto.SupplierID = newId;
                return dto;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> DeleteSupplier(int supplierid, UserDto currentUser)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "DELETE FROM dbo.Tbl_SparePartSuppliers WHERE SupplierID = @SupplierID";
            var rows = await connection.ExecuteAsync(sql, new { SupplierID = supplierid });
            return rows > 0;
        }
    }
}