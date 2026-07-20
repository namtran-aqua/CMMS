using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.Dtos.Common;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartService
{
    public partial class SparePartService
    {
        public async Task<SparePartDashboardDto> GetSparePartDashboardAsync(int? factoryId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var parameters = new DynamicParameters();
            
            var factoryFilter = "";
            if (factoryId.HasValue)
            {
                factoryFilter = " AND (FACID = @FactoryId)";
                parameters.Add("FactoryId", factoryId.Value);
            }

            var totalSKU = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM dbo.Tbl_SparePart WHERE 1=1 {factoryFilter}", parameters);
            var currentInventory = await connection.ExecuteScalarAsync<int>($"SELECT ISNULL(SUM(Inventory), 0) FROM dbo.Tbl_SparePart WHERE 1=1 {factoryFilter}", parameters);
            var inventoryValue = await connection.ExecuteScalarAsync<decimal>($"SELECT ISNULL(SUM(Inventory * Price), 0) FROM dbo.Tbl_SparePart WHERE 1=1 {factoryFilter}", parameters);
            var lowStockSKU = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM dbo.Tbl_SparePart WHERE Inventory <= MinStock {factoryFilter}", parameters);
            var zeroStockSKU = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM dbo.Tbl_SparePart WHERE Inventory = 0 {factoryFilter}", parameters);

            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var importFilter = "AND ImportDate >= @StartOfMonth";
            var exportFilter = "AND ExportDate >= @StartOfMonth";
            if (factoryId.HasValue)
            {
                importFilter += " AND FACID = @FactoryId";
                exportFilter += " AND FACID = @FactoryId";
            }
            parameters.Add("StartOfMonth", startOfMonth);

            var importThisMonth = await connection.ExecuteScalarAsync<int>(
                $"SELECT ISNULL(SUM(d.Quantity), 0) FROM dbo.Tbl_ImportOrderDetail d JOIN dbo.Tbl_ImportOrder o ON o.ImportID = d.ImportID WHERE o.Status = 'Completed' {importFilter}", parameters);
            
            var exportThisMonth = await connection.ExecuteScalarAsync<int>(
                $"SELECT ISNULL(SUM(d.Quantity), 0) FROM dbo.Tbl_ExportOrderDetail d JOIN dbo.Tbl_ExportOrder o ON o.ExportID = d.ExportID WHERE o.Status = 'Completed' {exportFilter}", parameters);

            var topPartsSql = $@"
                SELECT TOP 5 p.PartCode, p.PartName, SUM(d.Quantity) AS QuantityUsed
                FROM dbo.Tbl_ExportOrderDetail d
                JOIN dbo.Tbl_ExportOrder o ON o.ExportID = d.ExportID
                JOIN dbo.Tbl_SparePart p ON p.SPID = d.SPID
                WHERE o.Status = 'Completed' {(factoryId.HasValue ? " AND o.FACID = @FactoryId" : "")}
                GROUP BY p.PartCode, p.PartName
                ORDER BY QuantityUsed DESC";
            var topUsed = (await connection.QueryAsync<TopUsedPartDto>(topPartsSql, parameters)).ToList();

            var agingSql = $@"
                SELECT 
                    CASE 
                        WHEN DATEDIFF(day, i.ImportDate, GETDATE()) <= 30 THEN '0-30 days'
                        WHEN DATEDIFF(day, i.ImportDate, GETDATE()) <= 60 THEN '31-60 days'
                        WHEN DATEDIFF(day, i.ImportDate, GETDATE()) <= 90 THEN '61-90 days'
                        ELSE '90+ days'
                    END AS [Range],
                    SUM(i.RemainingQuantity) AS Quantity,
                    SUM(i.RemainingQuantity * p.Price) AS Value
                FROM dbo.Tbl_SparePartItem i
                JOIN dbo.Tbl_SparePart p ON p.SPID = i.SPID
                WHERE i.RemainingQuantity > 0 AND i.Status = 'Available'
                {(factoryId.HasValue ? " AND i.FACID = @FactoryId" : "")}
                GROUP BY 
                    CASE 
                        WHEN DATEDIFF(day, i.ImportDate, GETDATE()) <= 30 THEN '0-30 days'
                        WHEN DATEDIFF(day, i.ImportDate, GETDATE()) <= 60 THEN '31-60 days'
                        WHEN DATEDIFF(day, i.ImportDate, GETDATE()) <= 90 THEN '61-90 days'
                        ELSE '90+ days'
                    END";
            
            var agingList = (await connection.QueryAsync<AgingDistributionDto>(agingSql, parameters)).ToList();

            var ranges = new[] { "0-30 days", "31-60 days", "61-90 days", "90+ days" };
            var agingResult = new List<AgingDistributionDto>();
            foreach (var r in ranges)
            {
                var match = agingList.FirstOrDefault(x => x.Range == r);
                agingResult.Add(match ?? new AgingDistributionDto { Range = r, Quantity = 0, Value = 0m });
            }

            var topVendorsSql = $@"
                SELECT TOP 5 v.VendorName, COUNT(o.ImportID) AS TransactionsCount, SUM(d.Quantity * d.Price) AS TotalValue
                FROM dbo.Tbl_ImportOrder o
                JOIN dbo.Tbl_ImportOrderDetail d ON d.ImportID = o.ImportID
                JOIN dbo.Tbl_Vendors v ON v.VendorID = o.VendorID
                WHERE o.Status = 'Completed' {(factoryId.HasValue ? " AND o.FACID = @FactoryId" : "")}
                GROUP BY v.VendorName
                ORDER BY TotalValue DESC";
            
            var topVendors = (await connection.QueryAsync<TopVendorDto>(topVendorsSql, parameters)).ToList();

            return new SparePartDashboardDto
            {
                TotalSKU = totalSKU,
                CurrentInventory = currentInventory,
                InventoryValue = inventoryValue,
                ImportThisMonth = importThisMonth,
                ExportThisMonth = exportThisMonth,
                LowStockSKU = lowStockSKU,
                ZeroStockSKU = zeroStockSKU,
                TopUsedParts = topUsed,
                AgingDistribution = agingResult,
                TopVendors = topVendors
            };
        }

        public async Task<byte[]> ExportInventoryToExcelAsync(int? factoryId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT 
                    p.PartCode, p.PartName, c.CategoryName, p.Unit, p.Price, p.Inventory, p.MinStock,
                    l.LocName AS Location, s.SupplierName, p.Note
                FROM dbo.Tbl_SparePart p
                LEFT JOIN dbo.Tbl_SparePartCategories c ON c.CategoryID = p.CategoryID
                LEFT JOIN dbo.Tbl_SparePartSuppliers s ON s.SupplierID = p.SupplierID
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                WHERE 1=1";
            
            var parameters = new DynamicParameters();
            if (factoryId.HasValue)
            {
                sql += " AND (p.FACID = @FactoryId OR l.FACID = @FactoryId)";
                parameters.Add("FactoryId", factoryId.Value);
            }
            sql += " ORDER BY p.PartCode";

            var items = (await connection.QueryAsync<dynamic>(sql, parameters)).ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Part Code,Part Name,Category,Unit,Price,Inventory,Min Stock,Location,Supplier,Note");

            foreach (var item in items)
            {
                string escape(object? val)
                {
                    if (val == null) return string.Empty;
                    var s = val.ToString() ?? string.Empty;
                    if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
                    {
                        return $"\"{s.Replace("\"", "\"\"")}\"";
                    }
                    return s;
                }

                sb.AppendLine($"{escape(item.PartCode)},{escape(item.PartName)},{escape(item.CategoryName)},{escape(item.Unit)},{item.Price},{item.Inventory},{item.MinStock},{escape(item.Location)},{escape(item.SupplierName)},{escape(item.Note)}");
            }

            var csvBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var result = new byte[bom.Length + csvBytes.Length];
            Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
            Buffer.BlockCopy(csvBytes, 0, result, bom.Length, csvBytes.Length);

            return result;
        }
    }
}
