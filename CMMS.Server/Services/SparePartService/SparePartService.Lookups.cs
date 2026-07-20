using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.Dtos.Equipment;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CMMS.Server.Services.SparePartService
{
    public partial class SparePartService
    {
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

        public async Task<List<MovementTypeDto>> GetMovementTypesAsync(int? factoryId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT MovementTypeID, MovementTypeName, FACID FROM dbo.Tbl_MovementType WHERE FACID IS NULL";
            if (factoryId.HasValue)
            {
                sql += " OR FACID = @FactoryId";
            }
            sql += " ORDER BY MovementTypeName";
            return (await connection.QueryAsync<MovementTypeDto>(sql, new { FactoryId = factoryId })).ToList();
        }
    }
}
