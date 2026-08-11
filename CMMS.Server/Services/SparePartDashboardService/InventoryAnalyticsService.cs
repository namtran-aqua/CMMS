using CMMS.Data.Connection;
using CMMS.Shared.Dtos.SpareParts.Dashboard;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartDashboardService
{
    public class InventoryAnalyticsService : IInventoryAnalyticsService
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public InventoryAnalyticsService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<SummaryDto> GetSummaryAsync(DashboardFilterDto filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT 
                    COUNT(1) AS TotalSKUs,
                    SUM(CASE WHEN Inventory > 0 THEN 1 ELSE 0 END) AS InStockSKUs,
                    SUM(CASE WHEN Inventory > 0 AND Inventory <= MinStock THEN 1 ELSE 0 END) AS LowStockSKUs,
                    SUM(CASE WHEN Inventory <= 0 THEN 1 ELSE 0 END) AS ZeroStockSKUs
                FROM dbo.Tbl_SparePart
                WHERE (@FactoryId IS NULL OR FACID = @FactoryId)";
                
            return await connection.QueryFirstOrDefaultAsync<SummaryDto>(sql, new { FactoryId = filter.FactoryId }) ?? new SummaryDto();
        }

        public async Task<List<TrendDto>> GetTrendsAsync(DashboardFilterDto filter)
        {
            return new List<TrendDto>();
        }

        public async Task<List<TopConsumedDto>> GetTopConsumedAsync(DashboardFilterDto filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT TOP 5
                    p.PartCode,
                    p.PartName,
                    d.DeptCode AS SectionName,
                    SUM(t.Quantity) AS Quantity
                FROM dbo.Tbl_Transactions t
                JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                LEFT JOIN dbo.vw_FactoryDepartment d ON d.DeptID = p.DeptID
                WHERE t.Type IN ('EXPORT', 'MAINTENANCE')
                  AND (@FactoryId IS NULL OR p.FACID = @FactoryId)
                GROUP BY p.PartCode, p.PartName, d.DeptCode
                ORDER BY Quantity DESC";
                
            var result = (await connection.QueryAsync<TopConsumedDto>(sql, new { FactoryId = filter.FactoryId })).ToList();
            
            var totalQty = result.Sum(x => x.Quantity);
            foreach (var item in result)
            {
                if (totalQty > 0)
                    item.Percentage = Math.Round((decimal)item.Quantity * 100 / (decimal)totalQty, 1);
                item.Trend = "Up";
            }
            return result;
        }

        public async Task<List<RecentMovementDto>> GetRecentMovementsAsync(DashboardFilterDto filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT TOP 10
                    'TR-' + CAST(t.TransID AS VARCHAR) AS TransactionCode,
                    p.PartCode,
                    CASE 
                        WHEN t.Type = 'IMPORT' THEN 0
                        WHEN t.Type = 'EXPORT' THEN 1
                        WHEN t.Type = 'MAINTENANCE' THEN 4
                        ELSE 2
                    END AS Type,
                    t.Quantity,
                    t.Date
                FROM dbo.Tbl_Transactions t
                JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                WHERE (@FactoryId IS NULL OR p.FACID = @FactoryId)
                ORDER BY t.Date DESC, t.TransID DESC";
                
            return (await connection.QueryAsync<RecentMovementDto>(sql, new { FactoryId = filter.FactoryId })).ToList();
        }

        public async Task<List<CategoryValueDto>> GetCategoryValuesAsync(DashboardFilterDto filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT 
                    COALESCE(c.CategoryName, 'Other') AS CategoryName,
                    SUM(CAST(p.Inventory AS decimal(18,2)) * CAST(p.Price AS decimal(18,2))) AS Value
                FROM dbo.Tbl_SparePart p
                LEFT JOIN dbo.Tbl_SparePartCategories c ON c.CategoryID = p.CategoryID
                WHERE p.Inventory > 0
                  AND (@FactoryId IS NULL OR p.FACID = @FactoryId)
                GROUP BY c.CategoryName
                ORDER BY Value DESC";
                
            return (await connection.QueryAsync<CategoryValueDto>(sql, new { FactoryId = filter.FactoryId })).ToList();
        }
    }
}
