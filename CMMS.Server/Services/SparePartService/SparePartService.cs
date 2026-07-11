using CMMS.Data.Connection;
using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.Dtos.Common;
using CMMS.Shared.Dtos.Equipment;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;

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
                    INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, RefCode, Note, CreateBy, CreateDate, MovementType)
                    VALUES (@SPID, @Type, @Qty, @Date, @RefCode, @Note, @CreateBy, @CreateDate, @MovementType)";
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
                        INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, RefCode, EQID, Equipment, Note, CreateBy, MovementType)
                        VALUES (@SPID, 'OUT', @Qty, @Date, @RefCode, @EQID, @Equipment, @Note, @CreateBy, 'MAINTENANCE')";
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

        public async Task<SparePartPagedResultDto> GetPagedAsync(
            int page, 
            int pageSize, 
            string? searchText, 
            int? categoryId, 
            string? stockStatus, 
            string? sortBy, 
            int? factoryId)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (factoryId.HasValue)
            {
                conditions.Add("(l.FACID = @FactoryId OR d.FACID = @FactoryId)");
                parameters.Add("FactoryId", factoryId.Value);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                conditions.Add("p.CategoryID = @CategoryId");
                parameters.Add("CategoryId", categoryId.Value);
            }

            if (!string.IsNullOrEmpty(stockStatus))
            {
                if (stockStatus == "Low")
                {
                    conditions.Add("p.Inventory <= p.MinStock");
                }
                else if (stockStatus == "InStock")
                {
                    conditions.Add("p.Inventory > p.MinStock");
                }
                else if (stockStatus == "Out")
                {
                    conditions.Add("p.Inventory <= 0");
                }
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                conditions.Add("(p.PartCode LIKE @SearchText OR p.PartName LIKE @SearchText OR l.LocName LIKE @SearchText OR s.SupplierName LIKE @SearchText)");
                parameters.Add("SearchText", $"%{searchText.Trim()}%");
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

            var orderClause = sortBy switch
            {
                "NameDesc" => "p.PartName DESC",
                "CodeAsc" => "p.PartCode ASC",
                "PriceAsc" => "p.Price ASC",
                "PriceDesc" => "p.Price DESC",
                "StockAsc" => "p.Inventory ASC",
                "StockDesc" => "p.Inventory DESC",
                _ => "p.PartName ASC"
            };

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.Tbl_SparePart p
                LEFT JOIN dbo.Tbl_SparePartCategories c ON c.CategoryID = p.CategoryID
                LEFT JOIN dbo.Tbl_SparePartSuppliers s ON s.SupplierID = p.SupplierID
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                LEFT JOIN dbo.vw_FactoryDepartment d ON d.DeptID = p.DeptID
                {whereClause}";

            int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            var lowStockConditions = new List<string> { "p.Inventory <= p.MinStock" };
            if (factoryId.HasValue)
            {
                lowStockConditions.Add("(l.FACID = @FactoryId OR d.FACID = @FactoryId)");
            }
            var lowStockWhere = "WHERE " + string.Join(" AND ", lowStockConditions);
            var lowStockSql = $@"
                SELECT COUNT(1) 
                FROM dbo.Tbl_SparePart p
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                LEFT JOIN dbo.vw_FactoryDepartment d ON d.DeptID = p.DeptID
                {lowStockWhere}";
            
            int lowStockCount = await connection.ExecuteScalarAsync<int>(lowStockSql, new { FactoryId = factoryId });

            int offset = (page - 1) * pageSize;
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);

            var sql = $@"
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
                {whereClause}
                ORDER BY {orderClause}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var items = await connection.QueryAsync<SparePartDto>(sql, parameters);

            return new SparePartPagedResultDto
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                LowStockCount = lowStockCount
            };
        }

        public async Task<PagedResultDto<SparePartTransactionDto>> GetTransactionHistoryPagedAsync(
            int page, 
            int pageSize, 
            string? searchText, 
            string? typeFilter, 
            int? factoryId)
        {
            using var connection = _connectionFactory.CreateConnection();

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (factoryId.HasValue)
            {
                conditions.Add("(l.FACID = @FactoryId OR d.FACID = @FactoryId)");
                parameters.Add("FactoryId", factoryId.Value);
            }

            if (!string.IsNullOrEmpty(typeFilter))
            {
                if (typeFilter == "MAINTENANCE")
                {
                    conditions.Add("t.MovementType = 'MAINTENANCE'");
                }
                else
                {
                    conditions.Add("t.Type = @TypeFilter");
                    parameters.Add("TypeFilter", typeFilter);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                conditions.Add("(p.PartCode LIKE @SearchText OR p.PartName LIKE @SearchText OR t.Equipment LIKE @SearchText OR t.Note LIKE @SearchText)");
                parameters.Add("SearchText", $"%{searchText.Trim()}%");
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.Tbl_Transactions t
                LEFT JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                LEFT JOIN dbo.vw_FactoryDepartment d ON d.DeptID = p.DeptID
                {whereClause}";

            int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            int offset = (page - 1) * pageSize;
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);

            var sql = $@"
                SELECT 
                    t.TransID, t.SPID, p.PartCode, p.PartName,
                    t.Type, t.Quantity, t.Date, t.EQID, t.Note, t.MTID, t.CreateBy, t.CreateDate, u.WorkDayId AS CreateUser,
                    COALESCE(l.FACID, d.FACID) AS FACID, t.MovementType
                FROM dbo.Tbl_Transactions t
                LEFT JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                LEFT JOIN dbo.vw_FactoryDepartment d ON d.DeptID = p.DeptID
                LEFT JOIN dbo.Tbl_User u ON t.CreateBy = u.Id
                LEFT JOIN dbo.Tbl_MaintenanceRecord m ON t.MTID = m.MTID
                {whereClause}
                ORDER BY t.TransID DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var items = await connection.QueryAsync<SparePartTransactionDto>(sql, parameters);

            return new PagedResultDto<SparePartTransactionDto>
            {
                Items = items.ToList(),
                TotalCount = totalCount
            };
        }

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

        public async Task<ImportResultDto> ImportSparePartsAsync(Stream fileStream, string fileName, UserDto currentUser)
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
                
                int colCode = headers.FindIndex(h => h.Equals("PartCode", StringComparison.OrdinalIgnoreCase));
                int colName = headers.FindIndex(h => h.Equals("PartName", StringComparison.OrdinalIgnoreCase));
                int colUnit = headers.FindIndex(h => h.Equals("Unit", StringComparison.OrdinalIgnoreCase));
                int colPrice = headers.FindIndex(h => h.Equals("Price", StringComparison.OrdinalIgnoreCase));
                int colInventory = headers.FindIndex(h => h.Equals("Inventory", StringComparison.OrdinalIgnoreCase));
                int colMinStock = headers.FindIndex(h => h.Equals("MinStock", StringComparison.OrdinalIgnoreCase));
                int colCategory = headers.FindIndex(h => h.Equals("CategoryName", StringComparison.OrdinalIgnoreCase));
                int colSupplier = headers.FindIndex(h => h.Equals("SupplierName", StringComparison.OrdinalIgnoreCase));
                int colLoc = headers.FindIndex(h => h.Equals("LocName", StringComparison.OrdinalIgnoreCase));
                int colDept = headers.FindIndex(h => h.Equals("DeptCode", StringComparison.OrdinalIgnoreCase));
                int colNote = headers.FindIndex(h => h.Equals("Note", StringComparison.OrdinalIgnoreCase));

                if (colCode == -1 || colName == -1)
                {
                    result.Success = false;
                    result.Message = "File import thiếu cột bắt buộc: PartCode, PartName.";
                    return result;
                }

                using var connection = _connectionFactory.CreateConnection();
                connection.Open();

                var categories = (await connection.QueryAsync<SparePartCategoryDto>("SELECT CategoryID, CategoryName FROM dbo.Tbl_SparePartCategories")).ToList();
                var suppliers = (await connection.QueryAsync<SparePartSupplierDto>("SELECT SupplierID, SupplierName FROM dbo.Tbl_SparePartSuppliers")).ToList();
                var locations = (await connection.QueryAsync<LocationDto>("SELECT LocID, LocName FROM dbo.Tbl_FactoryLocation")).ToList();
                var departments = (await connection.QueryAsync<DepartmentDto>("SELECT DeptID, DeptCode FROM dbo.vw_FactoryDepartment")).ToList();

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
                            result.Errors.Add($"Dòng {i + 1}: Mã hoặc tên phụ tùng không được trống.");
                            continue;
                        }

                        var unit = colUnit != -1 ? fields[colUnit].Trim() : "";
                        decimal? price = null;
                        if (colPrice != -1 && decimal.TryParse(fields[colPrice], out var parsedPrice)) price = parsedPrice;
                        
                        int inventory = 0;
                        if (colInventory != -1 && int.TryParse(fields[colInventory], out var parsedInv)) inventory = parsedInv;

                        int minStock = 0;
                        if (colMinStock != -1 && int.TryParse(fields[colMinStock], out var parsedMin)) minStock = parsedMin;

                        string note = colNote != -1 ? fields[colNote].Trim() : "";

                        int? categoryId = null;
                        if (colCategory != -1)
                        {
                            var catName = fields[colCategory].Trim();
                            if (!string.IsNullOrEmpty(catName))
                            {
                                var cat = categories.FirstOrDefault(c => c.CategoryName.Equals(catName, StringComparison.OrdinalIgnoreCase));
                                if (cat == null)
                                {
                                    var insertCatSql = @"
                                        INSERT INTO dbo.Tbl_SparePartCategories (CategoryName, CreateDate, CreateBy) 
                                        VALUES (@CategoryName, @CreateDate, @CreateBy);
                                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                                    var newCatId = await connection.ExecuteScalarAsync<int>(insertCatSql, new {
                                        CategoryName = catName,
                                        CreateDate = DateTime.Now,
                                        CreateBy = currentUser?.Id
                                    });
                                    var newCat = new SparePartCategoryDto { CategoryID = newCatId, CategoryName = catName };
                                    categories.Add(newCat);
                                    categoryId = newCatId;
                                }
                                else
                                {
                                    categoryId = cat.CategoryID;
                                }
                            }
                        }

                        int? supplierId = null;
                        if (colSupplier != -1)
                        {
                            var supName = fields[colSupplier].Trim();
                            if (!string.IsNullOrEmpty(supName))
                            {
                                var sup = suppliers.FirstOrDefault(s => s.SupplierName.Equals(supName, StringComparison.OrdinalIgnoreCase));
                                if (sup == null)
                                {
                                    var insertSupSql = @"
                                        INSERT INTO dbo.Tbl_SparePartSuppliers (SupplierName, CreateDate, CreateBy) 
                                        VALUES (@SupplierName, @CreateDate, @CreateBy);
                                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                                    var newSupId = await connection.ExecuteScalarAsync<int>(insertSupSql, new {
                                        SupplierName = supName,
                                        CreateDate = DateTime.Now,
                                        CreateBy = currentUser?.Id
                                    });
                                    var newSup = new SparePartSupplierDto { SupplierID = newSupId, SupplierName = supName };
                                    suppliers.Add(newSup);
                                    supplierId = newSupId;
                                }
                                else
                                {
                                    supplierId = sup.SupplierID;
                                }
                            }
                        }

                        int? locId = null;
                        if (colLoc != -1)
                        {
                            var locName = fields[colLoc].Trim();
                            if (!string.IsNullOrEmpty(locName))
                            {
                                var loc = locations.FirstOrDefault(l => l.LocName != null && l.LocName.Equals(locName, StringComparison.OrdinalIgnoreCase));
                                if (loc != null) locId = loc.LocID;
                            }
                        }

                        int? deptId = null;
                        if (colDept != -1)
                        {
                            var deptCode = fields[colDept].Trim();
                            if (!string.IsNullOrEmpty(deptCode))
                            {
                                var dept = departments.FirstOrDefault(d => d.DeptCode != null && d.DeptCode.Equals(deptCode, StringComparison.OrdinalIgnoreCase));
                                if (dept != null) deptId = dept.DeptID;
                            }
                        }

                        var existingSp = await connection.QueryFirstOrDefaultAsync<int?>(
                            "SELECT SPID FROM dbo.Tbl_SparePart WHERE PartCode = @PartCode",
                            new { PartCode = code });

                        if (existingSp.HasValue)
                        {
                            var oldInventory = await connection.QueryFirstOrDefaultAsync<int>(
                                "SELECT ISNULL(Inventory, 0) FROM dbo.Tbl_SparePart WHERE SPID = @SPID",
                                new { SPID = existingSp.Value });

                            bool hasNewInventory = colInventory != -1 && !string.IsNullOrWhiteSpace(fields[colInventory]);

                            var updateSql = @"
                                UPDATE dbo.Tbl_SparePart
                                SET PartName = @PartName, CategoryID = @CategoryID, Unit = @Unit, Price = @Price,
                                    MinStock = @MinStock, LocID = @LocID, DeptID = @DeptID, SupplierID = @SupplierID,
                                    Note = @Note, UpdateDate = @UpdateDate, UpdateBy = @UpdateBy";
                            
                            if (hasNewInventory)
                            {
                                updateSql += ", Inventory = @Inventory";
                            }
                            updateSql += " WHERE SPID = @SPID";
                            
                            await connection.ExecuteAsync(updateSql, new {
                                SPID = existingSp.Value,
                                PartName = name,
                                CategoryID = categoryId,
                                Unit = unit,
                                Price = price,
                                MinStock = minStock,
                                LocID = locId,
                                DeptID = deptId,
                                SupplierID = supplierId,
                                Note = note,
                                UpdateDate = DateTime.Now,
                                UpdateBy = currentUser?.Id,
                                Inventory = inventory
                            });

                            if (hasNewInventory && inventory != oldInventory)
                            {
                                var delta = inventory - oldInventory;
                                var txType = delta > 0 ? "IN" : "OUT";
                                var txQty = Math.Abs(delta);

                                const string insertTxSql = @"
                                    INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, Note, CreateBy, CreateDate, MovementType)
                                    VALUES (@SPID, @Type, @Qty, @Date, @Note, @CreateBy, @CreateDate, 'IMPORT')";
                                
                                await connection.ExecuteAsync(insertTxSql, new {
                                    SPID = existingSp.Value,
                                    Type = txType,
                                    Qty = txQty,
                                    Date = DateTime.Now,
                                    Note = $"Cập nhật tồn kho qua Excel/CSV (Cũ: {oldInventory}, Mới: {inventory})",
                                    CreateBy = currentUser?.Id,
                                    CreateDate = DateTime.Now
                                });
                            }
                        }
                        else
                        {
                            var insertSql = @"
                                INSERT INTO dbo.Tbl_SparePart
                                    (PartCode, PartName, CategoryID, Unit, Price, Inventory, MinStock, LocID, DeptID, SupplierID, Note, CreateDate, UpdateDate, CreateBy)
                                VALUES
                                    (@PartCode, @PartName, @CategoryID, @Unit, @Price, @Inventory, @MinStock, @LocID, @DeptID, @SupplierID, @Note, @CreateDate, @UpdateDate, @CreateBy)";
                            
                            await connection.ExecuteAsync(insertSql, new {
                                PartCode = code,
                                PartName = name,
                                CategoryID = categoryId,
                                Unit = unit,
                                Price = price ?? 0m,
                                Inventory = inventory,
                                MinStock = minStock,
                                LocID = locId,
                                DeptID = deptId,
                                SupplierID = supplierId,
                                Note = note,
                                CreateDate = DateTime.Now,
                                UpdateDate = DateTime.Now,
                                CreateBy = currentUser?.Id
                            });

                            if (inventory > 0)
                            {
                                var getNewSpId = await connection.ExecuteScalarAsync<int>("SELECT SPID FROM dbo.Tbl_SparePart WHERE PartCode = @PartCode", new { PartCode = code });
                                const string insertTxSql = @"
                                    INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, Note, CreateBy, CreateDate, MovementType)
                                    VALUES (@SPID, 'IN', @Qty, @Date, @Note, @CreateBy, @CreateDate, 'IMPORT')";
                                await connection.ExecuteAsync(insertTxSql, new {
                                    SPID = getNewSpId,
                                    Qty = inventory,
                                    Date = DateTime.Now,
                                    Note = "Nhập kho ban đầu qua Excel/CSV",
                                    CreateBy = currentUser?.Id,
                                    CreateDate = DateTime.Now
                                });
                            }
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