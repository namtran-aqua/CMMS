using CMMS.Data.Connection;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace CMMS.Server.Services.Barcode
{
    public class BarcodeIdService : IBarcodeIdService
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        private const string CreateTableSql = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Tbl_BarcodeSequence' AND xtype='U')
            BEGIN
                CREATE TABLE dbo.Tbl_BarcodeSequence (
                    Department NVARCHAR(50) NOT NULL,
                    ItemType   NVARCHAR(50) NOT NULL,
                    LastNumber INT          NOT NULL DEFAULT 0,
                    CONSTRAINT PK_BarcodeSequence PRIMARY KEY (Department, ItemType)
                );
            END";

        public BarcodeIdService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// G?i m?t l?n lúc startup d? d?m b?o b?ng t?n t?i tru?c khi có request d?u tiên.
        /// </summary>
        public async Task EnsureTableExistsAsync()
        {
            await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();

            // Ensure Tbl_BarcodeSequence exists
            await connection.ExecuteAsync(CreateTableSql);

            // Ensure SparePartBarcode column exists on Tbl_SparePartItem (migration)
            await connection.ExecuteAsync(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.Tbl_SparePartItem') AND name = 'SparePartBarcode'
                )
                BEGIN
                    ALTER TABLE dbo.Tbl_SparePartItem ADD SparePartBarcode NVARCHAR(50) NULL;
                END");
        }

        private async Task<string> GenerateBarcodeIdAsync(string departmentCode, string itemType)
        {
            string prefix = $"VF{departmentCode.ToUpper()}{itemType.ToUpper()}";

            await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var sqlCheck = @"
                    SELECT LastNumber FROM dbo.Tbl_BarcodeSequence WITH (UPDLOCK, SERIALIZABLE)
                    WHERE Department = @Department AND ItemType = @ItemType";

                var lastNumber = await connection.QuerySingleOrDefaultAsync<int?>(
                    sqlCheck,
                    new { Department = departmentCode, ItemType = itemType },
                    transaction);

                int nextNumber;
                if (lastNumber.HasValue)
                {
                    nextNumber = lastNumber.Value + 1;
                    await connection.ExecuteAsync(
                        "UPDATE dbo.Tbl_BarcodeSequence SET LastNumber = @NextNumber WHERE Department = @Department AND ItemType = @ItemType",
                        new { NextNumber = nextNumber, Department = departmentCode, ItemType = itemType },
                        transaction);
                }
                else
                {
                    nextNumber = 1;
                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.Tbl_BarcodeSequence (Department, ItemType, LastNumber) VALUES (@Department, @ItemType, @NextNumber)",
                        new { Department = departmentCode, ItemType = itemType, NextNumber = nextNumber },
                        transaction);
                }

                transaction.Commit();
                return $"{prefix}{nextNumber:D6}";
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public Task<string> GenerateEquipmentBarcodeIdAsync(string departmentCode = "MNT")
            => GenerateBarcodeIdAsync(departmentCode, "EQ");

        public Task<string> GenerateSparePartBarcodeIdAsync(string departmentCode = "MNT")
            => GenerateBarcodeIdAsync(departmentCode, "SP");
    }
}



