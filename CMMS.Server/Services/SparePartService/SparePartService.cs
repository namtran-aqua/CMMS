using CMMS.Data.Connection;
using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.Dtos.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CMMS.Server.Services.SparePartService
{
    public partial class SparePartService : ISparePartService
    {
        private readonly IConfiguration _config;
        private readonly ISqlConnectionFactory _connectionFactory;

        public SparePartService(IConfiguration config, ISqlConnectionFactory connectionFactory)
        {
            _config = config;
            _connectionFactory = connectionFactory;
        }

        public async Task<List<SparePartDto>> GetAllAsync(int? factoryId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    p.SPID, p.PartCode, p.PartName, p.CategoryID, c.CategoryName,
                    p.Unit, p.Price, p.Inventory, p.MinStock, p.LocID, l.LocName AS Location,
                    p.SupplierID, s.SupplierName, p.CreateDate, p.UpdateDate,
                    p.IsCoded, p.ImageUrl, COALESCE(p.FACID, l.FACID, d.FACID) AS FACID
                FROM dbo.Tbl_SparePart p
                LEFT JOIN dbo.Tbl_SparePartCategories c ON c.CategoryID = p.CategoryID
                LEFT JOIN dbo.Tbl_SparePartSuppliers s ON s.SupplierID = p.SupplierID
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                LEFT JOIN dbo.vw_FactoryDepartment d ON d.DeptID = p.DeptID
                WHERE (@FactoryId IS NULL OR COALESCE(p.FACID, l.FACID, d.FACID) = @FactoryId)
                ORDER BY p.PartName";

            var parts = await connection.QueryAsync<SparePartDto>(sql, new { FactoryId = factoryId });
            return parts.ToList();
        }

        public async Task<SparePartDto> CreateAsync(SparePartDto dto, UserDto currentUser)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            const string sqlCheckCode = "SELECT COUNT(1) FROM dbo.Tbl_SparePart WHERE PartCode = @PartCode";
            const string sqlInsert = @"
                INSERT INTO dbo.Tbl_SparePart
                    (PartCode, PartName, CategoryID, Unit, Price, Inventory, MinStock, LocID, DeptID, SupplierID, Note, CreateDate, UpdateDate, CreateBy, IsCoded, ImageUrl, FACID)
                VALUES
                    (@PartCode, @PartName, @CategoryID, @Unit, @Price, @Inventory, @MinStock, @LocID, @DeptID, @SupplierID, @Note, @CreateDate, @UpdateDate, @CreateBy, @IsCoded, @ImageUrl, @FACID);
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

                cmd.Parameters.Add("@Inventory", SqlDbType.Int).Value = dto.Inventory ?? 0;
                cmd.Parameters.Add("@MinStock", SqlDbType.Int).Value = dto.MinStock ?? 0;
                cmd.Parameters.Add("@LocID", SqlDbType.Int).Value = (object?)dto.LocID ?? DBNull.Value;
                cmd.Parameters.Add("@DeptID", SqlDbType.Int).Value = (object?)dto.DeptID ?? DBNull.Value;
                cmd.Parameters.Add("@SupplierID", SqlDbType.Int).Value = (object?)dto.SupplierID ?? DBNull.Value;
                cmd.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = (object?)dto.Note ?? DBNull.Value;
                cmd.Parameters.Add("@CreateDate", SqlDbType.Date).Value = DateTime.Today;
                cmd.Parameters.Add("@UpdateDate", SqlDbType.Date).Value = DateTime.Today;
                cmd.Parameters.Add("@CreateBy", SqlDbType.UniqueIdentifier).Value = (object?)currentUser?.Id ?? DBNull.Value;
                cmd.Parameters.Add("@IsCoded", SqlDbType.Bit).Value = dto.IsCoded;
                cmd.Parameters.Add("@ImageUrl", SqlDbType.NVarChar, 500).Value = (object?)dto.ImageUrl ?? DBNull.Value;
                cmd.Parameters.Add("@FACID", SqlDbType.Int).Value = (object?)dto.FACID ?? DBNull.Value;

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
                    SupplierID = @SupplierID, Note = @Note, UpdateDate = @UpdateDate, UpdateBy = @UpdateBy,
                    IsCoded = @IsCoded, ImageUrl = @ImageUrl, FACID = @FACID
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

            cmd.Parameters.Add("@MinStock", SqlDbType.Int).Value = dto.MinStock ?? 0;
            cmd.Parameters.Add("@LocID", SqlDbType.Int).Value = (object?)dto.LocID ?? DBNull.Value;
            cmd.Parameters.Add("@SupplierID", SqlDbType.Int).Value = (object?)dto.SupplierID ?? DBNull.Value;
            cmd.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = (object?)dto.Note ?? DBNull.Value;
            cmd.Parameters.Add("@UpdateDate", SqlDbType.Date).Value = DateTime.Today;
            cmd.Parameters.Add("@UpdateBy", SqlDbType.UniqueIdentifier).Value = (object?)currentUser?.Id ?? DBNull.Value;
            cmd.Parameters.Add("@IsCoded", SqlDbType.Bit).Value = dto.IsCoded;
            cmd.Parameters.Add("@ImageUrl", SqlDbType.NVarChar, 500).Value = (object?)dto.ImageUrl ?? DBNull.Value;
            cmd.Parameters.Add("@FACID", SqlDbType.Int).Value = (object?)dto.FACID ?? DBNull.Value;

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

        public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> MovementTypeCache = new();

        public async Task<int?> GetMovementTypeIdByNameAsync(string name)
        {
            if (MovementTypeCache.TryGetValue(name, out var cachedId))
                return cachedId;

            using var connection = _connectionFactory.CreateConnection();
            var id = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT TOP 1 MovementTypeID FROM dbo.Tbl_MovementType WHERE MovementTypeName = @Name",
                new { Name = name });
            if (id.HasValue)
            {
                MovementTypeCache[name] = id.Value;
            }
            return id;
        }

        internal async Task<int?> GetMovementTypeIdByNameInternalAsync(IDbConnection connection, string name, IDbTransaction? transaction = null)
        {
            if (MovementTypeCache.TryGetValue(name, out var cachedId))
                return cachedId;

            var id = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT TOP 1 MovementTypeID FROM dbo.Tbl_MovementType WHERE MovementTypeName = @Name",
                new { Name = name },
                transaction);
            if (id.HasValue)
            {
                MovementTypeCache[name] = id.Value;
            }
            return id;
        }
    }
}