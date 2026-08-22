using CMMS.Data.Connection;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace CMMS.Server.Services.Barcode
{
    public class BarcodeIdService : IBarcodeIdService
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public BarcodeIdService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private async Task<string> GenerateBarcodeIdAsync(string departmentCode, string itemType)
        {
            string prefix = $"VF{departmentCode.ToUpper()}{itemType.ToUpper()}";

            await using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var sqlCheck = "SELECT LastNumber FROM Tbl_BarcodeSequence WITH (UPDLOCK, SERIALIZABLE) WHERE Department = @Department AND ItemType = @ItemType";
                var lastNumber = await connection.QuerySingleOrDefaultAsync<int?>(sqlCheck, new { Department = departmentCode, ItemType = itemType }, transaction);

                int nextNumber = 1;
                if (lastNumber.HasValue)
                {
                    nextNumber = lastNumber.Value + 1;
                    var sqlUpdate = "UPDATE Tbl_BarcodeSequence SET LastNumber = @NextNumber WHERE Department = @Department AND ItemType = @ItemType";
                    await connection.ExecuteAsync(sqlUpdate, new { NextNumber = nextNumber, Department = departmentCode, ItemType = itemType }, transaction);
                }
                else
                {
                    var sqlInsert = "INSERT INTO Tbl_BarcodeSequence (Department, ItemType, LastNumber) VALUES (@Department, @ItemType, @NextNumber)";
                    await connection.ExecuteAsync(sqlInsert, new { Department = departmentCode, ItemType = itemType, NextNumber = nextNumber }, transaction);
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
        {
            return GenerateBarcodeIdAsync(departmentCode, "EQ");
        }

        public Task<string> GenerateSparePartBarcodeIdAsync(string departmentCode = "MNT")
        {
            return GenerateBarcodeIdAsync(departmentCode, "SP");
        }
    }
}
