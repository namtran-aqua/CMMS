using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.Dtos.Common;
using CMMS.Shared.Dtos.Equipment;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartService
{
    public partial class SparePartService
    {
        public async Task<List<SparePartTransactionDto>> GetTransactionHistoryAsync(int? factoryId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    t.TransID, t.SPID, p.PartCode, p.PartName,
                    t.Type, t.Quantity, t.Date, t.EQID, t.Note, t.MTID, t.CreateBy, t.MovementTypeID, m.MovementTypeName AS MovementType, t.RefCode, t.CreateDate, u.WorkDayId AS CreateUser,
                    COALESCE(p.FACID, l.FACID, d.FACID) AS FACID
                FROM dbo.Tbl_Transactions t
                LEFT JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                LEFT JOIN dbo.vw_FactoryDepartment d ON d.DeptID = p.DeptID
                LEFT JOIN dbo.Tbl_User u ON t.CreateBy = u.Id
                LEFT JOIN dbo.Tbl_MaintenanceRecord mr ON t.MTID = mr.MTID
                LEFT JOIN dbo.Tbl_MovementType m ON t.MovementTypeID = m.MovementTypeID
                WHERE (@FactoryId IS NULL OR COALESCE(p.FACID, l.FACID, d.FACID) = @FactoryId)
                ORDER BY t.TransID DESC";

            return (await connection.QueryAsync<SparePartTransactionDto>(sql, new { FactoryId = factoryId })).ToList();
        }

        public async Task<SparePartPagedResultDto> GetPagedAsync(
            int page, 
            int pageSize, 
            string? searchText, 
            int? categoryId, 
            string? stockStatus, 
            string? sortBy, 
            int? factoryId,
            string? partCode = null,
            string? partName = null,
            int? supplierId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (factoryId.HasValue)
            {
                conditions.Add("(p.FACID = @FactoryId OR l.FACID = @FactoryId OR d.FACID = @FactoryId)");
                parameters.Add("FactoryId", factoryId.Value);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                conditions.Add("p.CategoryID = @CategoryId");
                parameters.Add("CategoryId", categoryId.Value);
            }

            if (supplierId.HasValue && supplierId.Value > 0)
            {
                conditions.Add("p.SupplierID = @SupplierId");
                parameters.Add("SupplierId", supplierId.Value);
            }

            if (!string.IsNullOrEmpty(partCode))
            {
                conditions.Add("p.PartCode LIKE @PartCodeFilter");
                parameters.Add("PartCodeFilter", $"%{partCode.Trim()}%");
            }

            if (!string.IsNullOrEmpty(partName))
            {
                conditions.Add("p.PartName LIKE @PartNameFilter");
                parameters.Add("PartNameFilter", $"%{partName.Trim()}%");
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
                _ => "p.CreateDate DESC, p.SPID DESC"
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
                lowStockConditions.Add("(p.FACID = @FactoryId OR l.FACID = @FactoryId OR d.FACID = @FactoryId)");
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
                    p.IsCoded, p.ImageUrl, COALESCE(p.FACID, l.FACID, d.FACID) AS FACID
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
            int? factoryId,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            using var connection = _connectionFactory.CreateConnection();

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (factoryId.HasValue)
            {
                conditions.Add("(l.FACID = @FactoryId OR d.FACID = @FactoryId OR p.FACID = @FactoryId)");
                parameters.Add("FactoryId", factoryId.Value);
            }

            if (!string.IsNullOrEmpty(typeFilter))
            {
                if (typeFilter == "MAINTENANCE")
                {
                    var maintId = await GetMovementTypeIdByNameAsync(MovementTypeConstants.Maintenance);
                    conditions.Add("t.MovementTypeID = @MaintId");
                    parameters.Add("MaintId", maintId);
                }
                else
                {
                    conditions.Add("t.Type = @TypeFilter");
                    parameters.Add("TypeFilter", typeFilter);
                }
            }

            if (fromDate.HasValue)
            {
                conditions.Add("t.Date >= @FromDate");
                parameters.Add("FromDate", fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                conditions.Add("t.Date <= @ToDate");
                parameters.Add("ToDate", toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                conditions.Add("(p.PartCode LIKE @SearchText OR p.PartName LIKE @SearchText OR t.Equipment LIKE @SearchText OR t.Note LIKE @SearchText OR t.RefCode LIKE @SearchText)");
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
                    COALESCE(p.FACID, l.FACID, d.FACID) AS FACID, t.MovementTypeID, m.MovementTypeName AS MovementType, t.RefCode
                FROM dbo.Tbl_Transactions t
                LEFT JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                LEFT JOIN dbo.vw_FactoryDepartment d ON d.DeptID = p.DeptID
                LEFT JOIN dbo.Tbl_User u ON t.CreateBy = u.Id
                LEFT JOIN dbo.Tbl_MaintenanceRecord mr ON t.MTID = mr.MTID
                LEFT JOIN dbo.Tbl_MovementType m ON t.MovementTypeID = m.MovementTypeID
                {whereClause}
                ORDER BY t.Date DESC, t.TransID DESC
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
                                var loc = locations.FirstOrDefault(l => l.LocName.Equals(locName, StringComparison.OrdinalIgnoreCase));
                                if (loc != null) locId = loc.LocID;
                            }
                        }

                        int? deptId = null;
                        if (colDept != -1)
                        {
                            var deptCode = fields[colDept].Trim();
                            if (!string.IsNullOrEmpty(deptCode))
                            {
                                var dept = departments.FirstOrDefault(d => d.DeptCode.Equals(deptCode, StringComparison.OrdinalIgnoreCase));
                                if (dept != null) deptId = dept.DeptID;
                            }
                        }

                        // Check if part code already exists
                        var existsSql = "SELECT SPID FROM dbo.Tbl_SparePart WHERE PartCode = @PartCode";
                        var existingPartId = await connection.QueryFirstOrDefaultAsync<int?>(existsSql, new { PartCode = code });

                        if (existingPartId.HasValue)
                        {
                            // Update existing
                            var updateSql = @"
                                UPDATE dbo.Tbl_SparePart 
                                SET PartName = @PartName, Unit = @Unit, Price = COALESCE(@Price, Price), 
                                    Inventory = @Inventory, MinStock = @MinStock, CategoryID = COALESCE(@CategoryID, CategoryID),
                                    SupplierID = COALESCE(@SupplierID, SupplierID), LocID = COALESCE(@LocID, LocID), 
                                    DeptID = COALESCE(@DeptID, DeptID), Note = @Note, UpdateDate = GETDATE()
                                WHERE SPID = @SPID";
                            await connection.ExecuteAsync(updateSql, new {
                                PartName = name,
                                Unit = unit,
                                Price = price,
                                Inventory = inventory,
                                MinStock = minStock,
                                CategoryID = categoryId,
                                SupplierID = supplierId,
                                LocID = locId,
                                DeptID = deptId,
                                Note = note,
                                SPID = existingPartId.Value
                            });
                            result.SuccessCount++;
                        }
                        else
                        {
                            // Insert new
                            var insertSql = @"
                                INSERT INTO dbo.Tbl_SparePart (PartCode, PartName, Unit, Price, Inventory, MinStock, CategoryID, SupplierID, LocID, DeptID, Note, CreateDate, UpdateDate, CreateBy, IsCoded)
                                VALUES (@PartCode, @PartName, @Unit, @Price, @Inventory, @MinStock, @CategoryID, @SupplierID, @LocID, @DeptID, @Note, GETDATE(), GETDATE(), @CreateBy, 0)";
                            await connection.ExecuteAsync(insertSql, new {
                                PartCode = code,
                                PartName = name,
                                Unit = unit,
                                Price = price ?? 0m,
                                Inventory = inventory,
                                MinStock = minStock,
                                CategoryID = categoryId,
                                SupplierID = supplierId,
                                LocID = locId,
                                DeptID = deptId,
                                Note = note,
                                CreateBy = currentUser?.Id
                            });
                            result.SuccessCount++;
                        }
                    }
                    catch (Exception lineEx)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Dòng {i + 1}: {lineEx.Message}");
                    }
                }

                result.Success = true;
                result.Message = $"Import hoàn tất. Thành công: {result.SuccessCount}, Thất bại: {result.FailureCount}.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Lỗi hệ thống khi import: {ex.Message}";
            }
            return result;
        }

        public async Task<PagedResultDto<SparePartItemDto>> GetCodedSparePartsPagedAsync(
            int page, 
            int pageSize, 
            string? serialCode, 
            string? partCode, 
            string? partName, 
            string? status, 
            int? factoryId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            conditions.Add("i.HasCode = 1");

            if (factoryId.HasValue && factoryId.Value > 0)
            {
                conditions.Add("i.FACID = @FactoryId");
                parameters.Add("FactoryId", factoryId.Value);
            }
            if (!string.IsNullOrWhiteSpace(serialCode))
            {
                conditions.Add("i.SerialCode LIKE @SerialCode");
                parameters.Add("SerialCode", $"%{serialCode.Trim()}%");
            }
            if (!string.IsNullOrWhiteSpace(partCode))
            {
                conditions.Add("p.PartCode LIKE @PartCode");
                parameters.Add("PartCode", $"%{partCode.Trim()}%");
            }
            if (!string.IsNullOrWhiteSpace(partName))
            {
                conditions.Add("p.PartName LIKE @PartName");
                parameters.Add("PartName", $"%{partName.Trim()}%");
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                conditions.Add("i.Status = @Status");
                parameters.Add("Status", status);
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.Tbl_SparePartItem i
                JOIN dbo.Tbl_SparePart p ON p.SPID = i.SPID
                {whereClause}";
            
            int total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            int offset = (page - 1) * pageSize;
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);

            var sql = $@"
                SELECT i.ItemID, i.SPID, i.ImportID, i.ImportDetailID, i.HasCode, i.SerialCode, i.Quantity, i.RemainingQuantity, i.ImportDate, i.Status, i.CreateAt,
                       i.FACID, i.DeptID,
                       p.PartCode, p.PartName
                FROM dbo.Tbl_SparePartItem i
                JOIN dbo.Tbl_SparePart p ON p.SPID = i.SPID
                {whereClause}
                ORDER BY i.ImportDate DESC, i.ItemID DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var items = (await connection.QueryAsync<SparePartItemDto>(sql, parameters)).ToList();
            return new PagedResultDto<SparePartItemDto> { Items = items, TotalCount = total };
        }

        public async Task<List<SparePartTransactionDto>> GetSparePartMovementHistoryAsync(int spid)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    t.TransID, t.SPID, p.PartCode, p.PartName,
                    t.Type, t.Quantity, t.Date, t.EQID, t.Note, t.MTID, t.CreateBy, t.CreateDate, u.WorkDayId AS CreateUser,
                    t.RefCode, t.MovementTypeID, m.MovementTypeName AS MovementType, COALESCE(p.FACID, l.FACID, d.FACID) AS FACID
                FROM dbo.Tbl_Transactions t
                LEFT JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                LEFT JOIN dbo.Tbl_FactoryLocation l ON l.LocID = p.LocID
                LEFT JOIN dbo.vw_FactoryDepartment d ON d.DeptID = p.DeptID
                LEFT JOIN dbo.Tbl_User u ON t.CreateBy = u.Id
                LEFT JOIN dbo.Tbl_MovementType m ON t.MovementTypeID = m.MovementTypeID
                WHERE t.SPID = @SPID
                ORDER BY t.Date DESC, t.TransID DESC";

            return (await connection.QueryAsync<SparePartTransactionDto>(sql, new { SPID = spid })).ToList();
        }
    }
}
