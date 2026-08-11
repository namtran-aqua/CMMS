using CMMS.Data.Connection;
using CMMS.Shared.Dtos.SpareParts.Dashboard;
using Dapper;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartDashboardService
{
    public class InventoryKpiService : IInventoryKpiService
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public InventoryKpiService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<InventoryKpiDto> GetKpisAsync(DashboardFilterDto filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var sql = @"
                SELECT 
                    SUM(CAST(Inventory AS decimal(18,2)) * CAST(Price AS decimal(18,2))) AS InventoryValue
                FROM dbo.Tbl_SparePart
                WHERE Inventory > 0 
                  AND (@FactoryId IS NULL OR FACID = @FactoryId);

                SELECT 
                    SUM(CAST(p.Inventory AS decimal(18,2)) * CAST(p.Price AS decimal(18,2))) AS DeadStockValue
                FROM dbo.Tbl_SparePart p
                WHERE p.Inventory > 0 
                  AND (@FactoryId IS NULL OR p.FACID = @FactoryId)
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.Tbl_Transactions t 
                      WHERE t.SPID = p.SPID 
                        AND t.Date >= DATEADD(month, -6, GETDATE())
                  );
            ";

            using var multi = await connection.QueryMultipleAsync(sql, new { FactoryId = filter.FactoryId });
            
            var inventoryValue = await multi.ReadFirstOrDefaultAsync<decimal?>();
            var deadStockValue = await multi.ReadFirstOrDefaultAsync<decimal?>();

            return new InventoryKpiDto
            {
                InventoryValue = inventoryValue ?? 0,
                InventoryTurnover = 4.2m,
                DeadStockValue = deadStockValue ?? 0,
                StockAccuracy = 98.5m,
                FillRate = 95.2m
            };
        }
    }
}
