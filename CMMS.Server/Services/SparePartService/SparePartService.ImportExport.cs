using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.Dtos.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;

namespace CMMS.Server.Services.SparePartService
{
    public partial class SparePartService
    {
        public async Task<ImportOrderDto> CreateImportOrderAsync(ImportOrderDto dto, UserDto currentUser)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.Details == null || !dto.Details.Any()) throw new ArgumentException("Lệnh nhập phải chứa ít nhất một phụ tùng.");

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var countToday = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM dbo.Tbl_ImportOrder WHERE CAST(CreateAt AS DATE) = CAST(GETDATE() AS DATE)",
                    transaction: transaction);
                dto.ImportCode = $"IMP-{DateTime.Now:yyMMdd}-{(countToday + 1):D4}";
                dto.Status = "Completed";
                dto.CreateBy = currentUser?.Id;
                dto.CreateAt = DateTime.Now;

                const string sqlHeader = @"
                    INSERT INTO dbo.Tbl_ImportOrder (ImportCode, PONumber, VendorID, ImportDate, FACID, Status, CreateBy, CreateAt)
                    VALUES (@ImportCode, @PONumber, @VendorID, @ImportDate, @FACID, @Status, @CreateBy, @CreateAt);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";
                
                dto.ImportID = await connection.ExecuteScalarAsync<int>(sqlHeader, dto, transaction);

                foreach (var detail in dto.Details)
                {
                    detail.ImportID = dto.ImportID;

                    var sparePart = await connection.QueryFirstOrDefaultAsync<SparePartDto>(
                        "SELECT IsCoded, Inventory, PartCode, PartName FROM dbo.Tbl_SparePart WHERE SPID = @SPID",
                        new { SPID = detail.SPID },
                        transaction: transaction);
                    if (sparePart == null) throw new KeyNotFoundException($"Không tìm thấy phụ tùng SPID = {detail.SPID}.");

                    detail.HasCode = sparePart.IsCoded;

                    if (detail.HasCode)
                    {
                        if (string.IsNullOrWhiteSpace(detail.SerialCode))
                            throw new ArgumentException($"Phụ tùng '{sparePart.PartName}' yêu cầu nhập mã Serial/Code.");
                        
                        var existCount = await connection.ExecuteScalarAsync<int>(
                            "SELECT COUNT(1) FROM dbo.Tbl_SparePartItem WHERE SerialCode = @SerialCode AND Status = 'Available'",
                            new { SerialCode = detail.SerialCode },
                            transaction: transaction);
                        if (existCount > 0)
                            throw new InvalidOperationException($"Mã Serial/Code '{detail.SerialCode}' của phụ tùng '{sparePart.PartName}' đã tồn tại trong kho.");

                        detail.Quantity = 1;
                    }
                    else
                    {
                        if (detail.Quantity <= 0)
                            throw new ArgumentException($"Số lượng nhập cho phụ tùng '{sparePart.PartName}' phải lớn hơn 0.");
                        detail.SerialCode = null;
                    }

                    const string sqlDetail = @"
                        INSERT INTO dbo.Tbl_ImportOrderDetail (ImportID, SPID, HasCode, SerialCode, Quantity, Price)
                        VALUES (@ImportID, @SPID, @HasCode, @SerialCode, @Quantity, @Price);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    detail.DetailID = await connection.ExecuteScalarAsync<int>(sqlDetail, detail, transaction);

                    await CreateSparePartItemRecordInternalAsync(
                        connection,
                        transaction,
                        detail.SPID,
                        dto.ImportID,
                        detail.DetailID,
                        detail.SerialCode,
                        detail.Quantity,
                        dto.ImportDate,
                        currentUser?.Id);

                    const string sqlUpdateInventory = @"
                        UPDATE dbo.Tbl_SparePart
                        SET Inventory = ISNULL((SELECT SUM(RemainingQuantity) FROM dbo.Tbl_SparePartItem WHERE SPID = @SPID AND Status = 'Available'), 0), UpdateDate = GETDATE()
                        WHERE SPID = @SPID";
                    await connection.ExecuteAsync(sqlUpdateInventory, new { SPID = detail.SPID }, transaction);

                    var importMovementTypeId = await GetMovementTypeIdByNameInternalAsync(connection, MovementTypeConstants.Import, transaction);

                    const string sqlLogTx = @"
                        INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, Note, CreateBy, CreateDate, MovementType, MovementTypeID, RefCode)
                        VALUES (@SPID, 'IN', @Qty, @Date, @Note, @CreateBy, @CreateDate, 'IMPORT', @MovementTypeID, @RefCode)";
                    await connection.ExecuteAsync(sqlLogTx, new {
                        SPID = detail.SPID,
                        Qty = detail.Quantity,
                        Date = dto.ImportDate,
                        Note = $"Nhập kho theo lệnh {dto.ImportCode}" + (detail.HasCode ? $" (Serial: {detail.SerialCode})" : ""),
                        CreateBy = currentUser?.Id,
                        CreateDate = DateTime.Now,
                        MovementTypeID = importMovementTypeId,
                        RefCode = dto.ImportCode
                    }, transaction);
                }

                if (dto.AttachmentUrls != null && dto.AttachmentUrls.Any())
                {
                    foreach (var url in dto.AttachmentUrls)
                    {
                        const string sqlAttach = @"
                            INSERT INTO dbo.Tbl_Attachments (Id, FilePath, FileExtend, FileName, CreatedTime, FileSize, ReferenceType, ReferenceID)
                            VALUES (@Id, @FilePath, @FileExtend, @FileName, @CreatedTime, @FileSize, 'Import', @ReferenceID)";
                        
                        var fileName = Path.GetFileName(url);
                        var ext = Path.GetExtension(url);
                        await connection.ExecuteAsync(sqlAttach, new {
                            Id = Guid.NewGuid(),
                            FilePath = url,
                            FileExtend = ext,
                            FileName = fileName,
                            CreatedTime = DateTime.Now,
                            FileSize = 0L,
                            ReferenceID = dto.ImportID.ToString()
                        }, transaction);
                    }
                }

                transaction.Commit();
                return dto;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateImportOrderStatusAsync(int importId, string status, UserDto currentUser)
        {
            if (status != "Reversed")
                throw new ArgumentException("Chỉ hỗ trợ chuyển trạng thái sang Reversed.");

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var import = await connection.QueryFirstOrDefaultAsync<ImportOrderDto>(
                    "SELECT ImportID, ImportCode, Status FROM dbo.Tbl_ImportOrder WHERE ImportID = @ImportID",
                    new { ImportID = importId },
                    transaction: transaction);
                if (import == null) throw new KeyNotFoundException("Không tìm thấy lệnh nhập kho.");
                if (import.Status == "Reversed") throw new InvalidOperationException("Lệnh nhập kho này đã được đảo ngược trước đó.");

                var consumedCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM dbo.Tbl_SparePartItem WHERE ImportID = @ImportID AND (RemainingQuantity < Quantity OR Status <> 'Available')",
                    new { ImportID = importId },
                    transaction: transaction);
                if (consumedCount > 0)
                    throw new InvalidOperationException("Không thể đảo ngược lệnh nhập kho vì một số phụ tùng trong lệnh này đã được xuất kho hoặc chuyển trạng thái.");

                var details = await connection.QueryAsync<ImportOrderDetailDto>(
                    "SELECT SPID, Quantity, SerialCode, HasCode FROM dbo.Tbl_ImportOrderDetail WHERE ImportID = @ImportID",
                    new { ImportID = importId },
                    transaction: transaction);

                foreach (var detail in details)
                {
                    await connection.ExecuteAsync(
                        "UPDATE dbo.Tbl_SparePartItem SET RemainingQuantity = 0, Status = 'Returned', UpdateAt = GETDATE(), UpdateBy = @UpdateBy WHERE ImportID = @ImportID AND SPID = @SPID",
                        new { ImportID = importId, SPID = detail.SPID, UpdateBy = currentUser?.Id },
                        transaction: transaction);

                    await connection.ExecuteAsync(
                        "UPDATE dbo.Tbl_SparePart SET Inventory = ISNULL((SELECT SUM(RemainingQuantity) FROM dbo.Tbl_SparePartItem WHERE SPID = @SPID AND Status = 'Available'), 0), UpdateDate = GETDATE() WHERE SPID = @SPID",
                        new { SPID = detail.SPID },
                        transaction: transaction);

                    var reversalMovementTypeId = await GetMovementTypeIdByNameInternalAsync(connection, MovementTypeConstants.Reversal, transaction);

                    const string sqlLogTx = @"
                        INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, Note, CreateBy, CreateDate, MovementType, MovementTypeID, RefCode)
                        VALUES (@SPID, 'OUT', @Qty, @Date, @Note, @CreateBy, @CreateDate, 'REVERSAL', @MovementTypeID, @RefCode)";
                    await connection.ExecuteAsync(sqlLogTx, new {
                        SPID = detail.SPID,
                        Qty = detail.Quantity,
                        Date = DateTime.Now,
                        Note = $"Đảo ngược lệnh nhập kho {import.ImportCode}" + (detail.HasCode ? $" (Serial: {detail.SerialCode})" : ""),
                        CreateBy = currentUser?.Id,
                        CreateDate = DateTime.Now,
                        MovementTypeID = reversalMovementTypeId,
                        RefCode = import.ImportCode
                    }, transaction);
                }

                await connection.ExecuteAsync(
                    "UPDATE dbo.Tbl_ImportOrder SET Status = 'Reversed', UpdateBy = @UpdateBy, UpdateAt = GETDATE() WHERE ImportID = @ImportID",
                    new { ImportID = importId, UpdateBy = currentUser?.Id },
                    transaction: transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<PagedResultDto<ImportOrderDto>> GetImportOrdersPagedAsync(
            int page, 
            int pageSize, 
            string? importCode, 
            string? po, 
            int? vendorId, 
            DateTime? fromDate, 
            DateTime? toDate, 
            int? factoryId,
            Guid? createdBy = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (factoryId.HasValue)
            {
                conditions.Add("o.FACID = @FactoryId");
                parameters.Add("FactoryId", factoryId.Value);
            }
            if (vendorId.HasValue && vendorId.Value > 0)
            {
                conditions.Add("o.VendorID = @VendorId");
                parameters.Add("VendorId", vendorId.Value);
            }
            if (!string.IsNullOrWhiteSpace(importCode))
            {
                conditions.Add("o.ImportCode LIKE @ImportCode");
                parameters.Add("ImportCode", $"%{importCode.Trim()}%");
            }
            if (!string.IsNullOrWhiteSpace(po))
            {
                conditions.Add("o.PONumber LIKE @PO");
                parameters.Add("PO", $"%{po.Trim()}%");
            }
            if (fromDate.HasValue)
            {
                conditions.Add("o.ImportDate >= @FromDate");
                parameters.Add("FromDate", fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                conditions.Add("o.ImportDate <= @ToDate");
                parameters.Add("ToDate", toDate.Value.Date.AddDays(1).AddTicks(-1));
            }
            if (createdBy.HasValue)
            {
                conditions.Add("o.CreateBy = @CreatedBy");
                parameters.Add("CreatedBy", createdBy.Value);
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

            var countSql = $"SELECT COUNT(1) FROM dbo.Tbl_ImportOrder o {whereClause}";
            int total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            int offset = (page - 1) * pageSize;
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);

            var sql = $@"
                SELECT o.ImportID, o.ImportCode, o.PONumber, o.VendorID, o.ImportDate, o.FACID, o.Status, o.CreateBy, o.CreateAt,
                       v.VendorName, u.WorkDayId AS CreateUser
                FROM dbo.Tbl_ImportOrder o
                LEFT JOIN dbo.Tbl_Vendors v ON v.VendorID = o.VendorID
                LEFT JOIN dbo.Tbl_User u ON u.Id = o.CreateBy
                {whereClause}
                ORDER BY o.CreateAt DESC, o.ImportID DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var items = (await connection.QueryAsync<ImportOrderDto>(sql, parameters)).ToList();
            if (items.Any())
            {
                var importIds = items.Select(x => x.ImportID.ToString()).ToList();
                var attachments = (await connection.QueryAsync<dynamic>(
                    "SELECT ReferenceID, FilePath FROM dbo.Tbl_Attachments WHERE ReferenceType = 'Import' AND ReferenceID IN @ImportIds",
                    new { ImportIds = importIds })).ToList();

                var attachGroup = attachments.GroupBy(x => x.ReferenceID).ToDictionary(g => g.Key, g => g.Select(x => (string)x.FilePath).ToList());
                foreach (var item in items)
                {
                    if (attachGroup.TryGetValue(item.ImportID.ToString(), out var urls))
                    {
                        item.AttachmentUrls = urls;
                    }
                }
            }
            return new PagedResultDto<ImportOrderDto> { Items = items, TotalCount = total };
        }

        public async Task<ImportOrderDto?> GetImportOrderDetailAsync(int importId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sqlHeader = @"
                SELECT o.ImportID, o.ImportCode, o.PONumber, o.VendorID, o.ImportDate, o.FACID, o.Status, o.CreateBy, o.CreateAt,
                       v.VendorName, u.WorkDayId AS CreateUser
                FROM dbo.Tbl_ImportOrder o
                LEFT JOIN dbo.Tbl_Vendors v ON v.VendorID = o.VendorID
                LEFT JOIN dbo.Tbl_User u ON u.Id = o.CreateBy
                WHERE o.ImportID = @ImportID";

            var header = await connection.QueryFirstOrDefaultAsync<ImportOrderDto>(sqlHeader, new { ImportID = importId });
            if (header == null) return null;

            var sqlDetails = @"
                SELECT d.DetailID, d.ImportID, d.SPID, d.HasCode, d.SerialCode, d.Quantity, d.Price,
                       p.PartCode, p.PartName
                FROM dbo.Tbl_ImportOrderDetail d
                JOIN dbo.Tbl_SparePart p ON p.SPID = d.SPID
                WHERE d.ImportID = @ImportID";

            header.Details = (await connection.QueryAsync<ImportOrderDetailDto>(sqlDetails, new { ImportID = importId })).ToList();

            var sqlAttachments = "SELECT FilePath FROM dbo.Tbl_Attachments WHERE ReferenceType = 'Import' AND ReferenceID = @ReferenceID";
            header.AttachmentUrls = (await connection.QueryAsync<string>(sqlAttachments, new { ReferenceID = importId.ToString() })).ToList();

            return header;
        }

        public async Task<ExportOrderDto> CreateExportOrderAsync(ExportOrderDto dto, UserDto currentUser)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.Details == null || !dto.Details.Any()) throw new ArgumentException("Lệnh xuất phải chứa ít nhất một phụ tùng.");

            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var result = await CreateExportOrderInternalAsync(connection, transaction, dto, currentUser);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<ExportOrderDto> CreateExportOrderInternalAsync(
            SqlConnection connection, 
            SqlTransaction transaction, 
            ExportOrderDto dto, 
            UserDto currentUser)
        {
            var countToday = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM dbo.Tbl_ExportOrder WHERE CAST(CreateAt AS DATE) = CAST(GETDATE() AS DATE)",
                transaction: transaction);
            dto.ExportCode = $"EXP-{DateTime.Now:yyMMdd}-{(countToday + 1):D4}";
            dto.Status = "Completed";
            dto.CreateBy = currentUser?.Id;
            dto.CreateAt = DateTime.Now;

            const string sqlHeader = @"
                INSERT INTO dbo.Tbl_ExportOrder (ExportCode, MovementTypeID, ExportDate, FACID, Status, CreateBy, CreateAt)
                VALUES (@ExportCode, @MovementTypeID, @ExportDate, @FACID, @Status, @CreateBy, @CreateAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            dto.ExportID = await connection.ExecuteScalarAsync<int>(sqlHeader, dto, transaction);

            var movementTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT MovementTypeName FROM dbo.Tbl_MovementType WHERE MovementTypeID = @MovementTypeID",
                new { MovementTypeID = dto.MovementTypeID },
                transaction: transaction) ?? "UNKNOWN";

            foreach (var detail in dto.Details)
            {
                detail.ExportID = dto.ExportID;

                var sparePart = await connection.QueryFirstOrDefaultAsync<SparePartDto>(
                    "SELECT IsCoded, Inventory, PartCode, PartName FROM dbo.Tbl_SparePart WHERE SPID = @SPID",
                    new { SPID = detail.SPID },
                    transaction: transaction);
                if (sparePart == null) throw new KeyNotFoundException($"Không tìm thấy phụ tùng SPID = {detail.SPID}.");

                detail.HasCode = sparePart.IsCoded;

                if (detail.HasCode)
                {
                    if (string.IsNullOrWhiteSpace(detail.SerialCode))
                        throw new ArgumentException($"Phụ tùng có code '{sparePart.PartName}' phải chọn mã Serial/Code.");
                    
                    detail.Quantity = 1;

                    var item = await connection.QueryFirstOrDefaultAsync<SparePartItemDto>(
                        "SELECT ItemID, Status, RemainingQuantity FROM dbo.Tbl_SparePartItem WHERE SPID = @SPID AND SerialCode = @SerialCode AND Status = 'Available'",
                        new { SPID = detail.SPID, SerialCode = detail.SerialCode },
                        transaction: transaction);

                    if (item == null || item.RemainingQuantity <= 0)
                        throw new InvalidOperationException($"Phụ tùng '{sparePart.PartName}' có Serial '{detail.SerialCode}' không có sẵn trong kho.");

                    const string sqlDetail = @"
                        INSERT INTO dbo.Tbl_ExportOrderDetail (ExportID, SPID, HasCode, SerialCode, Quantity)
                        VALUES (@ExportID, @SPID, @HasCode, @SerialCode, @Quantity);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    detail.DetailID = await connection.ExecuteScalarAsync<int>(sqlDetail, detail, transaction);

                    await connection.ExecuteAsync(
                        "UPDATE dbo.Tbl_SparePartItem SET RemainingQuantity = 0, Status = 'Reserved', UpdateAt = GETDATE(), UpdateBy = @UpdateBy WHERE ItemID = @ItemID",
                        new { ItemID = item.ItemID, UpdateBy = currentUser?.Id },
                        transaction: transaction);

                    await connection.ExecuteAsync(
                        "INSERT INTO dbo.Tbl_ExportItemMapping (ExportID, ExportDetailID, ItemID, Quantity) VALUES (@ExportID, @ExportDetailID, @ItemID, 1)",
                        new { ExportID = dto.ExportID, ExportDetailID = detail.DetailID, ItemID = item.ItemID },
                        transaction: transaction);
                }
                else
                {
                    if (detail.Quantity <= 0)
                        throw new ArgumentException($"Số lượng xuất cho '{sparePart.PartName}' phải lớn hơn 0.");

                    var currentStock = await connection.ExecuteScalarAsync<int>(
                        "SELECT ISNULL(SUM(RemainingQuantity), 0) FROM dbo.Tbl_SparePartItem WHERE SPID = @SPID AND Status = 'Available'",
                        new { SPID = detail.SPID },
                        transaction: transaction);

                    if (currentStock < detail.Quantity)
                        throw new InvalidOperationException($"Không đủ tồn kho cho phụ tùng '{sparePart.PartName}'. Hiện tại chỉ còn {currentStock}, yêu cầu xuất {detail.Quantity}.");

                    const string sqlDetail = @"
                        INSERT INTO dbo.Tbl_ExportOrderDetail (ExportID, SPID, HasCode, SerialCode, Quantity)
                        VALUES (@ExportID, @SPID, @HasCode, NULL, @Quantity);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    detail.DetailID = await connection.ExecuteScalarAsync<int>(sqlDetail, detail, transaction);

                    var availableItems = (await connection.QueryAsync<SparePartItemDto>(
                        "SELECT ItemID, RemainingQuantity, ImportDate FROM dbo.Tbl_SparePartItem WHERE SPID = @SPID AND Status = 'Available' AND RemainingQuantity > 0 ORDER BY ImportDate ASC, ItemID ASC",
                        new { SPID = detail.SPID },
                        transaction: transaction)).ToList();

                    int remainingToExport = detail.Quantity;
                    foreach (var item in availableItems)
                    {
                        if (remainingToExport <= 0) break;

                        int qtyToTake = Math.Min(item.RemainingQuantity, remainingToExport);
                        int newRemaining = item.RemainingQuantity - qtyToTake;
                        string newStatus = newRemaining == 0 ? "Reserved" : "Available";

                        await connection.ExecuteAsync(
                            "UPDATE dbo.Tbl_SparePartItem SET RemainingQuantity = @Remaining, Status = @Status, UpdateAt = GETDATE(), UpdateBy = @UpdateBy WHERE ItemID = @ItemID",
                            new { Remaining = newRemaining, Status = newStatus, ItemID = item.ItemID, UpdateBy = currentUser?.Id },
                            transaction: transaction);

                        await connection.ExecuteAsync(
                            "INSERT INTO dbo.Tbl_ExportItemMapping (ExportID, ExportDetailID, ItemID, Quantity) VALUES (@ExportID, @ExportDetailID, @ItemID, @Qty)",
                            new { ExportID = dto.ExportID, ExportDetailID = detail.DetailID, ItemID = item.ItemID, Qty = qtyToTake },
                            transaction: transaction);

                        remainingToExport -= qtyToTake;
                    }
                }

                await connection.ExecuteAsync(
                    "UPDATE dbo.Tbl_SparePart SET Inventory = ISNULL((SELECT SUM(RemainingQuantity) FROM dbo.Tbl_SparePartItem WHERE SPID = @SPID AND Status = 'Available'), 0), UpdateDate = GETDATE() WHERE SPID = @SPID",
                    new { SPID = detail.SPID },
                    transaction: transaction);

                const string sqlLogTx = @"
                    INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, Note, CreateBy, CreateDate, MovementType, MovementTypeID, RefCode)
                    VALUES (@SPID, 'OUT', @Qty, @Date, @Note, @CreateBy, @CreateDate, @MovementType, @MovementTypeID, @RefCode)";
                await connection.ExecuteAsync(sqlLogTx, new {
                    SPID = detail.SPID,
                    Qty = detail.Quantity,
                    Date = dto.ExportDate,
                    Note = $"Xuất kho theo lệnh {dto.ExportCode} ({movementTypeName})" + (detail.HasCode ? $" (Serial: {detail.SerialCode})" : ""),
                    CreateBy = currentUser?.Id,
                    CreateDate = DateTime.Now,
                    MovementType = movementTypeName,
                    MovementTypeID = dto.MovementTypeID,
                    RefCode = dto.ExportCode
                }, transaction);
            }

            if (dto.AttachmentUrls != null && dto.AttachmentUrls.Any())
            {
                foreach (var url in dto.AttachmentUrls)
                {
                    const string sqlAttach = @"
                        INSERT INTO dbo.Tbl_Attachments (Id, FilePath, FileExtend, FileName, CreatedTime, FileSize, ReferenceType, ReferenceID)
                        VALUES (@Id, @FilePath, @FileExtend, @FileName, @CreatedTime, @FileSize, 'Export', @ReferenceID)";
                    
                    var fileName = Path.GetFileName(url);
                    var ext = Path.GetExtension(url);
                    await connection.ExecuteAsync(sqlAttach, new {
                        Id = Guid.NewGuid(),
                        FilePath = url,
                        FileExtend = ext,
                        FileName = fileName,
                        CreatedTime = DateTime.Now,
                        FileSize = 0L,
                        ReferenceID = dto.ExportID.ToString()
                    }, transaction);
                }
            }

            return dto;
        }

        public async Task<bool> UpdateExportOrderStatusAsync(int exportId, string status, UserDto currentUser)
        {
            if (status != "Reversed")
                throw new ArgumentException("Chỉ hỗ trợ chuyển trạng thái sang Reversed.");

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var export = await connection.QueryFirstOrDefaultAsync<ExportOrderDto>(
                    "SELECT ExportID, ExportCode, Status FROM dbo.Tbl_ExportOrder WHERE ExportID = @ExportID",
                    new { ExportID = exportId },
                    transaction: transaction);
                if (export == null) throw new KeyNotFoundException("Không tìm thấy lệnh xuất kho.");
                if (export.Status == "Reversed") throw new InvalidOperationException("Lệnh xuất kho này đã được đảo ngược trước đó.");

                var details = await connection.QueryAsync<ExportOrderDetailDto>(
                    "SELECT SPID, Quantity, SerialCode, HasCode, DetailID FROM dbo.Tbl_ExportOrderDetail WHERE ExportID = @ExportID",
                    new { ExportID = exportId },
                    transaction: transaction);

                foreach (var detail in details)
                {
                    var mappings = await connection.QueryAsync<dynamic>(
                        "SELECT ItemID, Quantity FROM dbo.Tbl_ExportItemMapping WHERE ExportID = @ExportID AND ExportDetailID = @DetailID",
                        new { ExportID = exportId, DetailID = detail.DetailID },
                        transaction: transaction);

                    foreach (var map in mappings)
                    {
                        int itemId = map.ItemID;
                        int qty = map.Quantity;

                        await connection.ExecuteAsync(
                            "UPDATE dbo.Tbl_SparePartItem SET RemainingQuantity = RemainingQuantity + @Qty, Status = 'Available', UpdateAt = GETDATE(), UpdateBy = @UpdateBy WHERE ItemID = @ItemID",
                            new { Qty = qty, ItemID = itemId, UpdateBy = currentUser?.Id },
                            transaction: transaction);
                    }

                    await connection.ExecuteAsync(
                        "UPDATE dbo.Tbl_SparePart SET Inventory = ISNULL((SELECT SUM(RemainingQuantity) FROM dbo.Tbl_SparePartItem WHERE SPID = @SPID AND Status = 'Available'), 0), UpdateDate = GETDATE() WHERE SPID = @SPID",
                        new { SPID = detail.SPID },
                        transaction: transaction);

                    var reversalMovementTypeId = await GetMovementTypeIdByNameInternalAsync(connection, MovementTypeConstants.Reversal, transaction);

                    const string sqlLogTx = @"
                        INSERT INTO dbo.Tbl_Transactions (SPID, Type, Quantity, Date, Note, CreateBy, CreateDate, MovementType, MovementTypeID, RefCode)
                        VALUES (@SPID, 'IN', @Qty, @Date, @Note, @CreateBy, @CreateDate, 'REVERSAL', @MovementTypeID, @RefCode)";
                    await connection.ExecuteAsync(sqlLogTx, new {
                        SPID = detail.SPID,
                        Qty = detail.Quantity,
                        Date = DateTime.Now,
                        Note = $"Đảo ngược lệnh xuất kho {export.ExportCode}" + (detail.HasCode ? $" (Serial: {detail.SerialCode})" : ""),
                        CreateBy = currentUser?.Id,
                        CreateDate = DateTime.Now,
                        MovementTypeID = reversalMovementTypeId,
                        RefCode = export.ExportCode
                    }, transaction);
                }

                await connection.ExecuteAsync(
                    "UPDATE dbo.Tbl_ExportOrder SET Status = 'Reversed', UpdateBy = @UpdateBy, UpdateAt = GETDATE() WHERE ExportID = @ExportID",
                    new { ExportID = exportId, UpdateBy = currentUser?.Id },
                    transaction: transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<PagedResultDto<ExportOrderDto>> GetExportOrdersPagedAsync(
            int page, 
            int pageSize, 
            string? exportCode, 
            int? movementTypeId, 
            DateTime? fromDate, 
            DateTime? toDate, 
            int? factoryId,
            Guid? createdBy = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (factoryId.HasValue)
            {
                conditions.Add("o.FACID = @FactoryId");
                parameters.Add("FactoryId", factoryId.Value);
            }
            if (movementTypeId.HasValue && movementTypeId.Value > 0)
            {
                conditions.Add("o.MovementTypeID = @MovementTypeID");
                parameters.Add("MovementTypeID", movementTypeId.Value);
            }
            if (!string.IsNullOrWhiteSpace(exportCode))
            {
                conditions.Add("o.ExportCode LIKE @ExportCode");
                parameters.Add("ExportCode", $"%{exportCode.Trim()}%");
            }
            if (fromDate.HasValue)
            {
                conditions.Add("o.ExportDate >= @FromDate");
                parameters.Add("FromDate", fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                conditions.Add("o.ExportDate <= @ToDate");
                parameters.Add("ToDate", toDate.Value.Date.AddDays(1).AddTicks(-1));
            }
            if (createdBy.HasValue)
            {
                conditions.Add("o.CreateBy = @CreatedBy");
                parameters.Add("CreatedBy", createdBy.Value);
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

            var countSql = $"SELECT COUNT(1) FROM dbo.Tbl_ExportOrder o {whereClause}";
            int total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            int offset = (page - 1) * pageSize;
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);

            var sql = $@"
                SELECT o.ExportID, o.ExportCode, o.MovementTypeID, o.ExportDate, o.FACID, o.Status, o.CreateBy, o.CreateAt,
                       m.MovementTypeName, u.WorkDayId AS CreateUser
                FROM dbo.Tbl_ExportOrder o
                LEFT JOIN dbo.Tbl_MovementType m ON m.MovementTypeID = o.MovementTypeID
                LEFT JOIN dbo.Tbl_User u ON u.Id = o.CreateBy
                {whereClause}
                ORDER BY o.CreateAt DESC, o.ExportID DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var items = (await connection.QueryAsync<ExportOrderDto>(sql, parameters)).ToList();
            if (items.Any())
            {
                foreach (var item in items)
                {
                    var sqlAttach = @"
                        SELECT FilePath FROM dbo.Tbl_Attachments WHERE ReferenceType = 'Export' AND ReferenceID = @ReferenceID
                        UNION
                        SELECT FilePath FROM dbo.Tbl_Attachments a
                        JOIN dbo.Tbl_Transactions t ON a.MTID = t.MTID
                        WHERE t.RefCode = @ExportCode AND t.MTID IS NOT NULL";
                    item.AttachmentUrls = (await connection.QueryAsync<string>(sqlAttach, new { ReferenceID = item.ExportID.ToString(), ExportCode = item.ExportCode })).ToList();
                }
            }
            return new PagedResultDto<ExportOrderDto> { Items = items, TotalCount = total };
        }

        public async Task<ExportOrderDto?> GetExportOrderDetailAsync(int exportId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sqlHeader = @"
                SELECT o.ExportID, o.ExportCode, o.MovementTypeID, o.ExportDate, o.FACID, o.Status, o.CreateBy, o.CreateAt,
                       m.MovementTypeName, u.WorkDayId AS CreateUser
                FROM dbo.Tbl_ExportOrder o
                LEFT JOIN dbo.Tbl_MovementType m ON m.MovementTypeID = o.MovementTypeID
                LEFT JOIN dbo.Tbl_User u ON u.Id = o.CreateBy
                WHERE o.ExportID = @ExportID";

            var header = await connection.QueryFirstOrDefaultAsync<ExportOrderDto>(sqlHeader, new { ExportID = exportId });
            if (header == null) return null;

            var sqlDetails = @"
                SELECT d.DetailID, d.ExportID, d.SPID, d.HasCode, d.SerialCode, d.Quantity,
                       p.PartCode, p.PartName
                FROM dbo.Tbl_ExportOrderDetail d
                JOIN dbo.Tbl_SparePart p ON p.SPID = d.SPID
                WHERE d.ExportID = @ExportID";

            header.Details = (await connection.QueryAsync<ExportOrderDetailDto>(sqlDetails, new { ExportID = exportId })).ToList();

            var sqlAttachments = @"
                SELECT FilePath FROM dbo.Tbl_Attachments WHERE ReferenceType = 'Export' AND ReferenceID = @ReferenceID
                UNION
                SELECT FilePath FROM dbo.Tbl_Attachments a
                JOIN dbo.Tbl_Transactions t ON a.MTID = t.MTID
                WHERE t.RefCode = @ExportCode AND t.MTID IS NOT NULL";
            header.AttachmentUrls = (await connection.QueryAsync<string>(sqlAttachments, new { ReferenceID = exportId.ToString(), ExportCode = header.ExportCode })).ToList();

            return header;
        }

        public async Task<List<ImportOrderDto>> GetImportOrdersAllAsync(int? factoryId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT o.ImportID, o.ImportCode, o.PONumber, o.VendorID, o.ImportDate, o.FACID, o.Status, o.CreateBy, o.CreateAt,
                       v.VendorName, u.WorkDayId AS CreateUser
                FROM dbo.Tbl_ImportOrder o
                LEFT JOIN dbo.Tbl_Vendors v ON v.VendorID = o.VendorID
                LEFT JOIN dbo.Tbl_User u ON u.Id = o.CreateBy";
            if (factoryId.HasValue)
            {
                sql += " WHERE o.FACID = @FactoryId";
            }
            sql += " ORDER BY o.CreateAt DESC, o.ImportID DESC";

            var items = (await connection.QueryAsync<ImportOrderDto>(sql, new { FactoryId = factoryId })).ToList();
            if (items.Any())
            {
                var importIds = items.Select(x => x.ImportID.ToString()).ToList();
                var attachments = (await connection.QueryAsync<dynamic>(
                    "SELECT ReferenceID, FilePath FROM dbo.Tbl_Attachments WHERE ReferenceType = 'Import' AND ReferenceID IN @ImportIds",
                    new { ImportIds = importIds })).ToList();

                var attachGroup = attachments.GroupBy(x => x.ReferenceID).ToDictionary(g => g.Key, g => g.Select(x => (string)x.FilePath).ToList());
                foreach (var item in items)
                {
                    if (attachGroup.TryGetValue(item.ImportID.ToString(), out var urls))
                    {
                        item.AttachmentUrls = urls;
                    }
                }
            }
            return items;
        }

        public async Task<List<ExportOrderDto>> GetExportOrdersAllAsync(int? factoryId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT o.ExportID, o.ExportCode, o.MovementTypeID, o.ExportDate, o.FACID, o.Status, o.CreateBy, o.CreateAt,
                       m.MovementTypeName, u.WorkDayId AS CreateUser
                FROM dbo.Tbl_ExportOrder o
                LEFT JOIN dbo.Tbl_MovementType m ON m.MovementTypeID = o.MovementTypeID
                LEFT JOIN dbo.Tbl_User u ON u.Id = o.CreateBy";
            if (factoryId.HasValue)
            {
                sql += " WHERE o.FACID = @FactoryId";
            }
            sql += " ORDER BY o.CreateAt DESC, o.ExportID DESC";

            var items = (await connection.QueryAsync<ExportOrderDto>(sql, new { FactoryId = factoryId })).ToList();
            if (items.Any())
            {
                foreach (var item in items)
                {
                    var sqlAttach = @"
                        SELECT FilePath FROM dbo.Tbl_Attachments WHERE ReferenceType = 'Export' AND ReferenceID = @ReferenceID
                        UNION
                        SELECT FilePath FROM dbo.Tbl_Attachments a
                        JOIN dbo.Tbl_Transactions t ON a.MTID = t.MTID
                        WHERE t.RefCode = @ExportCode AND t.MTID IS NOT NULL";
                    item.AttachmentUrls = (await connection.QueryAsync<string>(sqlAttach, new { ReferenceID = item.ExportID.ToString(), ExportCode = item.ExportCode })).ToList();
                }
            }
            return items;
        }

        public async Task<List<SparePartItemDto>> GetCodedSparePartsAllAsync(int? factoryId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"
                SELECT i.ItemID, i.SPID, i.ImportID, i.ImportDetailID, i.HasCode, i.SerialCode, i.Quantity, i.RemainingQuantity, i.ImportDate, i.Status, i.CreateAt,
                       i.FACID, i.DeptID,
                       p.PartCode, p.PartName,
                       s.DaysInStock 
                FROM dbo.Tbl_SparePartItem i
                JOIN dbo.Tbl_SparePart p ON p.SPID = i.SPID
                JOIN dbo.vw_SparePartStockDetail s ON i.ItemID = s.ItemID
                WHERE i.HasCode = 1";
            if (factoryId.HasValue)
            {
                sql += " AND i.FACID = @FactoryId";
            }
            sql += " ORDER BY i.ImportDate DESC, i.ItemID DESC";

            return (await connection.QueryAsync<SparePartItemDto>(sql, new { FactoryId = factoryId })).ToList();
        }
    }
}
