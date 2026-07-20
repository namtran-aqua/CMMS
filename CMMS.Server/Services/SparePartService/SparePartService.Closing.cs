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

namespace CMMS.Server.Services.SparePartService
{
    public partial class SparePartService
    {
        public async Task<bool> ClosePeriodAsync(int year, int month, UserDto currentUser)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var parts = (await connection.QueryAsync<SparePartDto>(
                    "SELECT SPID, PartCode, PartName, Price, Inventory FROM dbo.Tbl_SparePart",
                    transaction: transaction)).ToList();

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddTicks(-1);

                var prevYear = month == 1 ? year - 1 : year;
                var prevMonth = month == 1 ? 12 : month - 1;

                foreach (var part in parts)
                {
                    var prevPeriod = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT ClosingQty, ClosingValue FROM dbo.Tbl_SparePartMonthlyPeriod WHERE Year = @Year AND Month = @Month AND SPID = @SPID",
                        new { Year = prevYear, Month = prevMonth, SPID = part.SPID },
                        transaction: transaction);

                    int openingQty = prevPeriod?.ClosingQty ?? 0;
                    decimal openingValue = prevPeriod?.ClosingValue ?? 0m;

                    int importQty = await connection.ExecuteScalarAsync<int>(
                        @"SELECT ISNULL(SUM(d.Quantity), 0)
                          FROM dbo.Tbl_ImportOrderDetail d
                          JOIN dbo.Tbl_ImportOrder o ON o.ImportID = d.ImportID
                          WHERE d.SPID = @SPID 
                            AND o.ImportDate BETWEEN @Start AND @End 
                            AND o.Status = 'Completed' 
                            AND o.PONumber IS NOT NULL",
                        new { SPID = part.SPID, Start = startDate, End = endDate },
                        transaction: transaction);

                    int exportQty = await connection.ExecuteScalarAsync<int>(
                        @"SELECT ISNULL(SUM(d.Quantity), 0)
                          FROM dbo.Tbl_ExportOrderDetail d
                          JOIN dbo.Tbl_ExportOrder o ON o.ExportID = d.ExportID
                          LEFT JOIN dbo.Tbl_MovementType m ON m.MovementTypeID = o.MovementTypeID
                          WHERE d.SPID = @SPID 
                            AND o.ExportDate BETWEEN @Start AND @End 
                            AND o.Status = 'Completed'
                            AND (m.MovementTypeName IS NULL OR (m.MovementTypeName <> 'Manual Adjust (IN)' AND m.MovementTypeName <> 'Manual Adjust (OUT)'))",
                        new { SPID = part.SPID, Start = startDate, End = endDate },
                        transaction: transaction);

                    int manualImportQty = await connection.ExecuteScalarAsync<int>(
                        @"SELECT ISNULL(SUM(d.Quantity), 0)
                          FROM dbo.Tbl_ImportOrderDetail d
                          JOIN dbo.Tbl_ImportOrder o ON o.ImportID = d.ImportID
                          WHERE d.SPID = @SPID 
                            AND o.ImportDate BETWEEN @Start AND @End 
                            AND o.Status = 'Completed' 
                            AND o.PONumber IS NULL",
                        new { SPID = part.SPID, Start = startDate, End = endDate },
                        transaction: transaction);

                    int manualExportQty = await connection.ExecuteScalarAsync<int>(
                        @"SELECT ISNULL(SUM(d.Quantity), 0)
                          FROM dbo.Tbl_ExportOrderDetail d
                          JOIN dbo.Tbl_ExportOrder o ON o.ExportID = d.ExportID
                          JOIN dbo.Tbl_MovementType m ON m.MovementTypeID = o.MovementTypeID
                          WHERE d.SPID = @SPID 
                            AND o.ExportDate BETWEEN @Start AND @End 
                            AND o.Status = 'Completed'
                            AND (m.MovementTypeName = 'Manual Adjust (IN)' OR m.MovementTypeName = 'Manual Adjust (OUT)')",
                        new { SPID = part.SPID, Start = startDate, End = endDate },
                        transaction: transaction);

                    int adjustOrderInQty = await connection.ExecuteScalarAsync<int>(
                        @"SELECT ISNULL(SUM(d.Quantity), 0)
                          FROM dbo.Tbl_AdjustOrderDetail d
                          JOIN dbo.Tbl_AdjustOrder o ON o.AdjustID = d.AdjustID
                          WHERE d.SPID = @SPID 
                            AND o.AdjustDate BETWEEN @Start AND @End 
                            AND d.Type = 'IN'",
                        new { SPID = part.SPID, Start = startDate, End = endDate },
                        transaction: transaction);

                    int adjustOrderOutQty = await connection.ExecuteScalarAsync<int>(
                        @"SELECT ISNULL(SUM(d.Quantity), 0)
                          FROM dbo.Tbl_AdjustOrderDetail d
                          JOIN dbo.Tbl_AdjustOrder o ON o.AdjustID = d.AdjustID
                          WHERE d.SPID = @SPID 
                            AND o.AdjustDate BETWEEN @Start AND @End 
                            AND d.Type = 'OUT'",
                        new { SPID = part.SPID, Start = startDate, End = endDate },
                        transaction: transaction);

                    int adjustmentQty = manualImportQty - manualExportQty + adjustOrderInQty - adjustOrderOutQty;

                    int closingQty = openingQty + importQty - exportQty + adjustmentQty;
                    decimal closingValue = closingQty * (part.Price ?? 0m);

                    const string sqlSave = @"
                        IF EXISTS (SELECT 1 FROM dbo.Tbl_SparePartMonthlyPeriod WHERE Year = @Year AND Month = @Month AND SPID = @SPID)
                        BEGIN
                            UPDATE dbo.Tbl_SparePartMonthlyPeriod
                            SET OpeningQty = @OpeningQty, OpeningValue = @OpeningValue, ImportQty = @ImportQty, ExportQty = @ExportQty, 
                                AdjustmentQty = @AdjustmentQty, ClosingQty = @ClosingQty, ClosingValue = @ClosingValue,
                                UpdateBy = @UpdateBy, UpdateAt = GETDATE()
                            WHERE Year = @Year AND Month = @Month AND SPID = @SPID
                        END
                        ELSE
                        BEGIN
                            INSERT INTO dbo.Tbl_SparePartMonthlyPeriod 
                                (Year, Month, SPID, OpeningQty, OpeningValue, ImportQty, ExportQty, AdjustmentQty, ClosingQty, ClosingValue, CreateBy, CreateAt)
                            VALUES 
                                (@Year, @Month, @SPID, @OpeningQty, @OpeningValue, @ImportQty, @ExportQty, @AdjustmentQty, @ClosingQty, @ClosingValue, @CreateBy, GETDATE())
                        END";

                    await connection.ExecuteAsync(sqlSave, new {
                        Year = year,
                        Month = month,
                        SPID = part.SPID,
                        OpeningQty = openingQty,
                        OpeningValue = openingValue,
                        ImportQty = importQty,
                        ExportQty = exportQty,
                        AdjustmentQty = adjustmentQty,
                        ClosingQty = closingQty,
                        ClosingValue = closingValue,
                        CreateBy = currentUser?.Id,
                        UpdateBy = currentUser?.Id
                    }, transaction: transaction);
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<PagedResultDto<SparePartMonthlyPeriodDto>> GetMonthlyPeriodsPagedAsync(
            int page, 
            int pageSize, 
            int? year, 
            int? month, 
            int? factoryId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (year.HasValue)
            {
                conditions.Add("p.Year = @Year");
                parameters.Add("Year", year.Value);
            }
            if (month.HasValue)
            {
                conditions.Add("p.Month = @Month");
                parameters.Add("Month", month.Value);
            }
            if (factoryId.HasValue)
            {
                conditions.Add("sp.FACID = @FactoryId");
                parameters.Add("FactoryId", factoryId.Value);
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.Tbl_SparePartMonthlyPeriod p
                JOIN dbo.Tbl_SparePart sp ON sp.SPID = p.SPID
                {whereClause}";
            int total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            int offset = (page - 1) * pageSize;
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);

            var sql = $@"
                SELECT p.PeriodID, p.Year, p.Month, p.SPID, p.OpeningQty, p.OpeningValue, p.ImportQty, p.ExportQty, p.AdjustmentQty, p.ClosingQty, p.ClosingValue, p.CreateAt AS ClosingDate,
                       sp.PartCode, sp.PartName, u.WorkDayId AS CreateUser
                FROM dbo.Tbl_SparePartMonthlyPeriod p
                JOIN dbo.Tbl_SparePart sp ON sp.SPID = p.SPID
                LEFT JOIN dbo.Tbl_User u ON u.Id = p.CreateBy
                {whereClause}
                ORDER BY p.Year DESC, p.Month DESC, sp.PartName ASC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var items = (await connection.QueryAsync<SparePartMonthlyPeriodDto>(sql, parameters)).ToList();
            return new PagedResultDto<SparePartMonthlyPeriodDto> { Items = items, TotalCount = total };
        }

        public async Task<List<SparePartMonthlyPeriodDto>> GetMonthlyPeriodsAllAsync(int? factoryId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT p.PeriodID, p.Year, p.Month, p.SPID, p.OpeningQty, p.OpeningValue, p.ImportQty, p.ExportQty, p.AdjustmentQty, p.ClosingQty, p.ClosingValue, p.CreateAt AS ClosingDate,
                       sp.PartCode, sp.PartName, u.WorkDayId AS CreateUser
                FROM dbo.Tbl_SparePartMonthlyPeriod p
                JOIN dbo.Tbl_SparePart sp ON sp.SPID = p.SPID
                LEFT JOIN dbo.Tbl_User u ON u.Id = p.CreateBy";
            if (factoryId.HasValue)
            {
                sql += " WHERE sp.FACID = @FactoryId";
            }
            sql += " ORDER BY p.Year DESC, p.Month DESC, sp.PartName ASC";

            return (await connection.QueryAsync<SparePartMonthlyPeriodDto>(sql, new { FactoryId = factoryId })).ToList();
        }
    }
}
