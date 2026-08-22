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

        public async Task<DashboardDto> GetAdvancedDashboardAsync(DashboardFilterDto filter)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                -- 1. Summary
                DECLARE @CurrentMonthStart DATETIME = DATEADD(month, DATEDIFF(month, 0, GETDATE()), 0);
                DECLARE @PrevMonthStart DATETIME = DATEADD(month, -1, @CurrentMonthStart);
                
                SELECT 
                    ISNULL(SUM(CAST(Inventory AS decimal(18,2))), 0) AS TotalInventoryQuantity,
                    ISNULL(SUM(CAST(Inventory AS decimal(18,2)) * CAST(Price AS decimal(18,2))), 0) AS TotalInventoryValue,
                    ISNULL((SELECT SUM(Quantity) FROM dbo.Tbl_Transactions t JOIN dbo.Tbl_SparePart tp ON t.SPID = tp.SPID WHERE t.Type = 'IN' AND t.Date >= @CurrentMonthStart AND (@FactoryId IS NULL OR COALESCE(t.FACID, tp.FACID) = @FactoryId) AND (@DepartmentId IS NULL OR COALESCE(t.DeptID, tp.DeptID) = @DepartmentId)), 0) AS ImportThisMonth,
                    ISNULL((SELECT SUM(Quantity) FROM dbo.Tbl_Transactions t JOIN dbo.Tbl_SparePart tp ON t.SPID = tp.SPID WHERE t.Type = 'IN' AND t.Date >= @PrevMonthStart AND t.Date < @CurrentMonthStart AND (@FactoryId IS NULL OR COALESCE(t.FACID, tp.FACID) = @FactoryId) AND (@DepartmentId IS NULL OR COALESCE(t.DeptID, tp.DeptID) = @DepartmentId)), 0) AS ImportPrevMonth,
                    ISNULL((SELECT SUM(Quantity) FROM dbo.Tbl_Transactions t JOIN dbo.Tbl_SparePart tp ON t.SPID = tp.SPID WHERE t.Type = 'OUT' AND t.Date >= @CurrentMonthStart AND (@FactoryId IS NULL OR COALESCE(t.FACID, tp.FACID) = @FactoryId) AND (@DepartmentId IS NULL OR COALESCE(t.DeptID, tp.DeptID) = @DepartmentId)), 0) AS ExportThisMonth,
                    ISNULL((SELECT SUM(Quantity) FROM dbo.Tbl_Transactions t JOIN dbo.Tbl_SparePart tp ON t.SPID = tp.SPID WHERE t.Type = 'OUT' AND t.Date >= @PrevMonthStart AND t.Date < @CurrentMonthStart AND (@FactoryId IS NULL OR COALESCE(t.FACID, tp.FACID) = @FactoryId) AND (@DepartmentId IS NULL OR COALESCE(t.DeptID, tp.DeptID) = @DepartmentId)), 0) AS ExportPrevMonth
                FROM dbo.Tbl_SparePart p
                WHERE (@FactoryId IS NULL OR p.FACID = @FactoryId) AND (@DepartmentId IS NULL OR p.DeptID = @DepartmentId);

                -- 2. Coded Ratio
                SELECT 
                    ISNULL(SUM(CASE WHEN IsCoded = 1 THEN 1 ELSE 0 END), 0) AS CodedCount,
                    ISNULL(SUM(CASE WHEN IsCoded = 0 THEN 1 ELSE 0 END), 0) AS NonCodedCount
                FROM dbo.Tbl_SparePart
                WHERE (@FactoryId IS NULL OR FACID = @FactoryId) AND (@DepartmentId IS NULL OR DeptID = @DepartmentId);

                -- 3. Stock Status
                SELECT 
                    ISNULL(SUM(CASE WHEN ISNULL(Inventory, 0) > ISNULL(MinStock, 0) THEN 1 ELSE 0 END), 0) AS HealthyStock,
                    ISNULL(SUM(CASE WHEN ISNULL(Inventory, 0) > 0 AND ISNULL(Inventory, 0) <= ISNULL(MinStock, 0) THEN 1 ELSE 0 END), 0) AS LowStock,
                    ISNULL(SUM(CASE WHEN ISNULL(Inventory, 0) <= 0 THEN 1 ELSE 0 END), 0) AS OutOfStock
                FROM dbo.Tbl_SparePart
                WHERE (@FactoryId IS NULL OR FACID = @FactoryId) AND (@DepartmentId IS NULL OR DeptID = @DepartmentId);

                -- 4. InOutTrends
                DECLARE @SixMonthsAgo DATETIME = DATEADD(month, -5, @CurrentMonthStart);
                SELECT 
                    YEAR(t.Date) AS Year,
                    MONTH(t.Date) AS Month,
                    COUNT(CASE WHEN t.Type = 'IN' THEN 1 END) AS ImportQuantity,
                    COUNT(CASE WHEN t.Type = 'OUT' THEN 1 END) AS ExportQuantity
                FROM dbo.Tbl_Transactions t
                JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                WHERE t.Date >= @SixMonthsAgo
                  AND (@FactoryId IS NULL OR COALESCE(t.FACID, p.FACID) = @FactoryId) AND (@DepartmentId IS NULL OR COALESCE(t.DeptID, p.DeptID) = @DepartmentId)
                GROUP BY YEAR(t.Date), MONTH(t.Date)
                ORDER BY Year, Month;

                -- 5. Top Imported
                SELECT TOP 5
                    ISNULL(p.PartCode, '') AS PartCode,
                    ISNULL(p.PartName, '') AS PartName,
                    ISNULL(p.Unit, '') AS Unit,
                    SUM(t.Quantity) AS Quantity
                FROM dbo.Tbl_Transactions t
                JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                WHERE t.Type = 'IN' AND t.Date >= @CurrentMonthStart
                  AND (@FactoryId IS NULL OR COALESCE(t.FACID, p.FACID) = @FactoryId) AND (@DepartmentId IS NULL OR COALESCE(t.DeptID, p.DeptID) = @DepartmentId)
                GROUP BY p.SPID, p.PartCode, p.PartName, p.Unit
                ORDER BY Quantity DESC;

                -- 6. Top Exported
                SELECT TOP 5
                    ISNULL(p.PartCode, '') AS PartCode,
                    ISNULL(p.PartName, '') AS PartName,
                    ISNULL(p.Unit, '') AS Unit,
                    SUM(t.Quantity) AS Quantity
                FROM dbo.Tbl_Transactions t
                JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                WHERE t.Type = 'OUT' AND t.Date >= @CurrentMonthStart
                  AND (@FactoryId IS NULL OR COALESCE(t.FACID, p.FACID) = @FactoryId) AND (@DepartmentId IS NULL OR COALESCE(t.DeptID, p.DeptID) = @DepartmentId)
                GROUP BY p.SPID, p.PartCode, p.PartName, p.Unit
                ORDER BY Quantity DESC;

                -- 7. Low Stock By Location
                SELECT 
                    ISNULL(l.LocName, 'Unknown') AS Location,
                    COUNT(p.SPID) AS Count
                FROM dbo.Tbl_SparePart p
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                WHERE ISNULL(p.Inventory, 0) > 0 AND ISNULL(p.Inventory, 0) <= ISNULL(p.MinStock, 0)
                  AND (@FactoryId IS NULL OR p.FACID = @FactoryId) AND (@DepartmentId IS NULL OR p.DeptID = @DepartmentId)
                GROUP BY ISNULL(l.LocName, 'Unknown')
                ORDER BY Count DESC;

                -- 8. True Inventory Aging
                SELECT 
                    CASE 
                        WHEN DATEDIFF(day, ImportDate, GETDATE()) <= 30 THEN '0-30'
                        WHEN DATEDIFF(day, ImportDate, GETDATE()) <= 60 THEN '31-60'
                        WHEN DATEDIFF(day, ImportDate, GETDATE()) <= 90 THEN '61-90'
                        ELSE '>90'
                    END AS Range,
                    SUM(RemainingQuantity) AS Quantity
                FROM dbo.Tbl_SparePartItem i
                JOIN dbo.Tbl_SparePart p ON p.SPID = i.SPID
                WHERE i.RemainingQuantity > 0 
                  AND (@FactoryId IS NULL OR i.FACID = @FactoryId) AND (@DepartmentId IS NULL OR p.DeptID = @DepartmentId)
                GROUP BY 
                    CASE 
                        WHEN DATEDIFF(day, ImportDate, GETDATE()) <= 30 THEN '0-30'
                        WHEN DATEDIFF(day, ImportDate, GETDATE()) <= 60 THEN '31-60'
                        WHEN DATEDIFF(day, ImportDate, GETDATE()) <= 90 THEN '61-90'
                        ELSE '>90'
                    END;

                -- 9. Aging Detailed Report
                SELECT TOP 10
                    ISNULL(p.PartCode, '') AS PartCode,
                    ISNULL(p.PartName, '') AS PartName,
                    SUM(i.RemainingQuantity) AS CurrentStock,
                    MIN(i.ImportDate) AS LastMovementDate,
                    MAX(DATEDIFF(day, i.ImportDate, GETDATE())) AS AgeDays
                FROM dbo.Tbl_SparePartItem i
                JOIN dbo.Tbl_SparePart p ON p.SPID = i.SPID
                WHERE i.RemainingQuantity > 0 
                  AND (@FactoryId IS NULL OR i.FACID = @FactoryId) AND (@DepartmentId IS NULL OR p.DeptID = @DepartmentId)
                GROUP BY p.SPID, p.PartCode, p.PartName
                ORDER BY AgeDays DESC;
            ";

            using var multi = await connection.QueryMultipleAsync(sql, new { FactoryId = filter.FactoryId, DepartmentId = filter.DepartmentId });

            var dashboard = new DashboardDto();

            // 1. Summary
            var summaryData = await multi.ReadFirstOrDefaultAsync();
            if (summaryData != null)
            {
                dashboard.Summary.TotalInventoryQuantity = summaryData.TotalInventoryQuantity;
                dashboard.Summary.TotalInventoryValue = summaryData.TotalInventoryValue;
                dashboard.Summary.ImportThisMonth = summaryData.ImportThisMonth;
                dashboard.Summary.ExportThisMonth = summaryData.ExportThisMonth;
                
                decimal importPrev = summaryData.ImportPrevMonth;
                if (importPrev == 0)
                    dashboard.Summary.ImportChangePercentage = null;
                else
                    dashboard.Summary.ImportChangePercentage = (double)Math.Round((summaryData.ImportThisMonth - importPrev) / importPrev * 100m, 1);

                decimal exportPrev = summaryData.ExportPrevMonth;
                if (exportPrev == 0)
                    dashboard.Summary.ExportChangePercentage = null;
                else
                    dashboard.Summary.ExportChangePercentage = (double)Math.Round((summaryData.ExportThisMonth - exportPrev) / exportPrev * 100m, 1);
            }

            // 2. Coded Ratio
            var codedRatio = await multi.ReadFirstOrDefaultAsync<CodedRatioDto>();
            if (codedRatio != null) dashboard.CodedRatio = codedRatio;

            // 3. Stock Status
            var stockStatus = await multi.ReadFirstOrDefaultAsync<StockStatusDto>();
            if (stockStatus != null) dashboard.StockStatus = stockStatus;

            // 4. InOutTrends
            var dbTrendRows = (await multi.ReadAsync<dynamic>()).ToList();
            var dbTrends = dbTrendRows.Select(row => new InOutTrendDto
            {
                Month = $"{row.Year}-{(int)row.Month:D2}",
                ImportQuantity = row.ImportQuantity,
                ExportQuantity = row.ExportQuantity
            }).ToList();

            var last6Months = new List<string>();
            var now = DateTime.Now;
            for (int i = 5; i >= 0; i--)
            {
                last6Months.Add(now.AddMonths(-i).ToString("yyyy-MM"));
            }

            foreach (var month in last6Months)
            {
                var existing = dbTrends.FirstOrDefault(t => t.Month == month);
                if (existing != null)
                {
                    dashboard.InOutTrends.Add(existing);
                }
                else
                {
                    dashboard.InOutTrends.Add(new InOutTrendDto { Month = month, ImportQuantity = 0, ExportQuantity = 0 });
                }
            }

            // 5. Top Imported
            dashboard.TopImported = (await multi.ReadAsync<TopTransactionDto>()).ToList();

            // 6. Top Exported
            dashboard.TopExported = (await multi.ReadAsync<TopTransactionDto>()).ToList();

            // 7. Low Stock By Location
            dashboard.LowStockByLocation = (await multi.ReadAsync<LowStockByLocationDto>()).ToList();

            // 8. Movement Aging
            var dbAging = (await multi.ReadAsync<AgingDistributionDto>()).ToList();
            var requiredRanges = new[] { "0-30", "31-60", "61-90", ">90" };
            foreach (var range in requiredRanges)
            {
                var existing = dbAging.FirstOrDefault(x => x.Range == range);
                if (existing != null)
                {
                    dashboard.MovementAging.Add(existing);
                }
                else
                {
                    dashboard.MovementAging.Add(new AgingDistributionDto { Range = range, Quantity = 0 });
                }
            }

            // 9. Aging Detailed Report
            dashboard.AgingReport = (await multi.ReadAsync<PartAgingDto>()).ToList();

            return dashboard;
        }
    }
}
