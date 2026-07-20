using AntDesign;
using CMMS.Client.Pages.Equipment;
using CMMS.Data.Connection;
using CMMS.Shared.Authorization;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Maintenance;
using CMMS.Shared.Dtos.User;
using Dapper;
using Microsoft.Data.SqlClient;
using NPOI.SS.Formula.Functions;
using CMMS.Shared.Dtos.Maintenance.Attachments;
using System.Data;
using System.Security.Claims;

namespace CMMS.Server.Services.MaintenanceService
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IConfiguration _config;
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly SparePartService.ISparePartService _sparePartService;

        public MaintenanceService(IConfiguration config, ISqlConnectionFactory connectionFactory, SparePartService.ISparePartService sparePartService)
        {
            _config = config;
            _connectionFactory = connectionFactory;
            _sparePartService = sparePartService;
        }
        public async Task<List<MaintenanceDto>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT 
                    m.MTID, m.EQID, m.UpdateBy, m.UpdateTime, m.StsMainID, m.MaintDate, 
                    m.VendorID, m.MaintPrice, m.PICID, m.MaintPIC, m.MaintDescription, m.MaintNote, m.IsEQActive,
                    a.Id, a.MTID, a.FilePath, a.FileExtend, a.FileName, a.FileSize, a.CreatedTime
                FROM Tbl_MaintenanceRecord m
                LEFT JOIN dbo.Tbl_Attachments a
                ON a.MTID = m.MTID";

            var maintenanceDict = new Dictionary<long, MaintenanceDto>();

            await connection.QueryAsync<MaintenanceDto, AttachmentDto, MaintenanceDto>(
                sql,
                (maint, att) =>
                {
                    if (!maintenanceDict.TryGetValue(maint.MTID, out var maintEntry))
                    {
                        maintEntry = maint;
                        maintEntry.Attachments = new List<AttachmentDto>();
                        maintenanceDict.Add(maintEntry.MTID, maintEntry);
                    }

                    if (att != null && att.Id != Guid.Empty)
                    {
                        maintEntry.Attachments.Add(att);
                    }

                    return maint;
                },
                splitOn: "Id");

            var maintTypeId = await _sparePartService.GetMovementTypeIdByNameAsync(CMMS.Shared.Dtos.SpareParts.MovementTypeConstants.Maintenance);

            var sparePartsSql = @"
                SELECT 
                    t.MTID, t.SPID, t.Quantity AS Qty, p.PartCode, p.PartName, p.Unit
                FROM dbo.Tbl_Transactions t
                JOIN dbo.Tbl_SparePart p ON p.SPID = t.SPID
                WHERE t.MovementTypeID = @MaintId AND t.MTID IS NOT NULL";

            var sparePartsList = await connection.QueryAsync<MaintenanceSparePartDtoHelper>(sparePartsSql, new { MaintId = maintTypeId });
            var sparePartsGrouped = sparePartsList.GroupBy(x => x.MTID);

            foreach (var group in sparePartsGrouped)
            {
                if (maintenanceDict.TryGetValue(group.Key, out var maintEntry))
                {
                    maintEntry.SpareParts = group.Select(x => new MaintenanceSparePartDto
                    {
                        SPID = x.SPID,
                        PartCode = x.PartCode,
                        PartName = x.PartName,
                        Unit = x.Unit,
                        Qty = x.Qty
                    }).ToList();
                }
            }

            return maintenanceDict.Values.OrderByDescending(m => m.MaintDate).ToList();
        }
        public async Task<bool> CreatedAsync(MaintenanceDto maintenance, int ID, UserDto currentUser)
        {
            using var checkConnection = _connectionFactory.CreateConnection();
            var original = await checkConnection.QueryFirstOrDefaultAsync<EquipmentDto>(
                "SELECT FACID, PICID, EquipmentName FROM dbo.vw_EquipmentInfo WHERE EQID = @EQID",
                new { EQID = ID });

            if (original == null)
                throw new KeyNotFoundException($"Không tìm thấy thiết bị EQID = {ID}.");

            if (!AuthorizationHelper.CanEditOrMaintain(currentUser, original.FACID, original.PICID))
                throw new UnauthorizedAccessException("Bạn không có quyền ghi nhận bảo trì thiết bị này.");

            var connStr = _config.GetConnectionString("DefaultConnection");

            const string sqlInsert = @"
                INSERT INTO dbo.Tbl_MaintenanceRecord 
                    (EQID, UpdateBy, UpdateTime, StsMainID, MaintDate, VendorID, MaintPrice, PICID, MaintPIC, MaintDescription, MaintNote, IsEQActive)
                VALUES 
                    (@EQID, @UpdateBy, @UpdateTime, @StsMainID, @MaintDate, @VendorID, @MaintPrice, @PICID, @MaintPIC, @MaintDescription, @MaintNote, @IsEQActive);
                SELECT CAST(SCOPE_IDENTITY() AS INT); ";

            const string sqlUpdateEquipment = @"
                UPDATE dbo.Tbl_EquipmentInfo
                SET LastMaintenanceDate = @MaintDate,
                    IsActive = @IsActive
                WHERE EQID = @EQID";

            const string sqlUploadImage = @"
                INSERT INTO dbo.Tbl_Attachments
                    (Id, FilePath, FileExtend, FileName, CreatedTime, FileSize, MTID)
                VALUES
                    (@Id, @FilePath, @FileExtend, @FileName, @CreatedTime, @FileSize, @MTID)";

            await using var con = new SqlConnection(connStr);
            await con.OpenAsync();

            // 👉 Dùng transaction để đảm bảo không bị lệch dữ liệu
            await using var tran = await con.BeginTransactionAsync();

            try
            {
                int newMTID;
                await using (var cmd = new SqlCommand(sqlInsert, con, (SqlTransaction)tran))
                {
                     cmd.Parameters.Add("@EQID", SqlDbType.Int).Value = ID;
                     cmd.Parameters.Add("@UpdateBy", SqlDbType.NVarChar, 50).Value = (object?)maintenance.WorkDayId ?? DBNull.Value;
                     cmd.Parameters.Add("@UpdateTime", SqlDbType.DateTime).Value = DateTime.Now;

                     cmd.Parameters.Add("@StsMainID", SqlDbType.Int).Value = (object?)maintenance.StsMainID ?? DBNull.Value;
                     cmd.Parameters.Add("@MaintDate", SqlDbType.DateTime).Value = (object?)maintenance.MaintDate ?? DBNull.Value;
                     cmd.Parameters.Add("@VendorID", SqlDbType.Int).Value = (object?)maintenance.VendorID ?? DBNull.Value;
                     cmd.Parameters.Add("@PICID", SqlDbType.NVarChar).Value = (object?)maintenance.PICID ?? DBNull.Value;

                     var priceParam = cmd.Parameters.Add("@MaintPrice", SqlDbType.Decimal);
                     priceParam.Precision = 18;
                     priceParam.Scale = 2;
                     priceParam.Value = (object?)maintenance.MaintPrice ?? DBNull.Value;

                     cmd.Parameters.Add("@MaintPIC", SqlDbType.NVarChar, 50).Value = (object?)maintenance.MaintPIC ?? DBNull.Value;
                     cmd.Parameters.Add("@MaintDescription", SqlDbType.NVarChar, 255).Value = (object?)maintenance.MaintDescription ?? DBNull.Value;
                     cmd.Parameters.Add("@MaintNote", SqlDbType.NVarChar, 255).Value = (object?)maintenance.MaintNote ?? DBNull.Value;
                     cmd.Parameters.Add("@IsEQActive", SqlDbType.Bit).Value = (object?)maintenance.IsEQActive ?? DBNull.Value;
                     
                     var result = await cmd.ExecuteScalarAsync();
                     newMTID = Convert.ToInt32(result);
                }

                await using (var cmd2 = new SqlCommand(sqlUpdateEquipment, con, (SqlTransaction)tran))
                {
                     cmd2.Parameters.Add("@EQID", SqlDbType.Int).Value = ID;
                     cmd2.Parameters.Add("@MaintDate", SqlDbType.DateTime).Value = (object?)maintenance.MaintDate ?? DBNull.Value;
                     cmd2.Parameters.Add("@IsActive", SqlDbType.Bit).Value = (object?)maintenance.IsEQActive ?? DBNull.Value;

                     await cmd2.ExecuteNonQueryAsync();
                }

                if (maintenance.Attachments != null && maintenance.Attachments.Any())
                {
                     foreach (var attachment in maintenance.Attachments)
                     {
                         await using var cmd3 = new SqlCommand(sqlUploadImage, con, (SqlTransaction)tran);
                         cmd3.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = attachment.Id == Guid.Empty ? Guid.NewGuid() : attachment.Id;
                         cmd3.Parameters.Add("@FilePath", SqlDbType.NVarChar, 500).Value = (object?)attachment.FilePath ?? DBNull.Value;
                         cmd3.Parameters.Add("@FileExtend", SqlDbType.NVarChar, 50).Value = (object?)attachment.FileExtend ?? DBNull.Value;
                         cmd3.Parameters.Add("@FileName", SqlDbType.NVarChar, 255).Value = (object?)attachment.FileName ?? DBNull.Value;
                         cmd3.Parameters.Add("@CreatedTime", SqlDbType.DateTime2).Value = attachment.CreatedTime == default ? DateTime.Now : attachment.CreatedTime;
                         cmd3.Parameters.Add("@FileSize", SqlDbType.BigInt).Value = attachment.FileSize;
                         cmd3.Parameters.Add("@MTID", SqlDbType.BigInt).Value = newMTID;
                         await cmd3.ExecuteNonQueryAsync();
                     }
                }

                if (maintenance.SpareParts != null && maintenance.SpareParts.Any(sp => sp.Qty > 0))
                {
                     var movementTypeId = await con.QueryFirstOrDefaultAsync<int?>(
                         "SELECT TOP 1 MovementTypeID FROM dbo.Tbl_MovementType WHERE MovementTypeName = @Name",
                         new { Name = CMMS.Shared.Dtos.SpareParts.MovementTypeConstants.Maintenance },
                         transaction: tran);

                     var exportDto = new CMMS.Shared.Dtos.SpareParts.ExportOrderDto
                     {
                         ExportDate = maintenance.MaintDate ?? DateTime.Now,
                         FACID = original.FACID,
                         Status = "Completed",
                         CreateBy = currentUser?.Id,
                         CreateAt = DateTime.Now,
                         MovementTypeID = movementTypeId ?? 1,
                         Details = maintenance.SpareParts.Where(sp => sp.Qty > 0).Select(sp => new CMMS.Shared.Dtos.SpareParts.ExportOrderDetailDto
                         {
                             SPID = sp.SPID,
                             PartCode = sp.PartCode,
                             PartName = sp.PartName,
                             HasCode = sp.HasCode,
                             SerialCode = sp.SerialCode,
                             Quantity = sp.Qty
                         }).ToList()
                     };

                     var createdExport = await _sparePartService.CreateExportOrderInternalAsync(con, (SqlTransaction)tran, exportDto, currentUser);

                     const string sqlUpdateTxMaint = @"
                         UPDATE dbo.Tbl_Transactions 
                         SET MTID = @MTID, EQID = @EQID, Equipment = @Equipment 
                         WHERE RefCode = @RefCode";
                     await con.ExecuteAsync(sqlUpdateTxMaint, new {
                         MTID = newMTID,
                         EQID = ID,
                         Equipment = original.EquipmentName ?? "",
                         RefCode = createdExport.ExportCode
                     }, transaction: tran);
                }

                await tran.CommitAsync();
                return true;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }
        //public async Task<bool> CreatedAsync(MaintenanceDto maintenance, int ID)
        //{
        //    var connStr = _config.GetConnectionString("DefaultConnection");
        //    const string sqlCreateMaintenance = @"
        //    INSERT INTO dbo.Tbl_MaintenanceRecord 
        //        (EQID, UpdateBy, UpdateTime, StsMainID, MaintDate, VendorID, MaintPrice, PICID, MaintPIC, MaintDescription, MaintNote)
        //    VALUES 
        //        (@EQID, @UpdateBy, @UpdateTime, @StsMainID, @MaintDate, @VendorID, @MaintPrice, @PICID, @MaintPIC, @MaintDescription, @MaintNote)";

        //    const string sqlUpdateEquipment = @"
        //        UPDATE dbo.Equipment
        //        SET LastMaintenanceDate = @MaintDate
        //        WHERE ID = @EQID";

        //    await using var con = new SqlConnection(connStr);
        //    await using var cmd = new SqlCommand(sqlCreateMaintenance, con);


        //    cmd.Parameters.Add("@EQID", SqlDbType.Int).Value = (object?)ID ?? DBNull.Value;
        //    cmd.Parameters.Add("@UpdateBy", SqlDbType.NVarChar, 50).Value = (object?)maintenance.UpdateBy ?? DBNull.Value;
        //    cmd.Parameters.Add("@UpdateTime", SqlDbType.DateTime).Value = DateTime.Now;
        //    cmd.Parameters.Add("@StsMainID", SqlDbType.Int).Value = (object?)maintenance.StsMainID ?? DBNull.Value;
        //    cmd.Parameters.Add("@MaintDate", SqlDbType.DateTime).Value = (object?)maintenance.MaintDate ?? DBNull.Value;
        //    cmd.Parameters.Add("@VendorID", SqlDbType.Int).Value = (object?)maintenance.VendorID ?? DBNull.Value;
        //    cmd.Parameters.Add("@PICID", SqlDbType.Int).Value = (object?)maintenance.PICID ?? DBNull.Value;
        //    cmd.Parameters.Add("@MaintPrice", SqlDbType.Decimal).Value = (object?)maintenance.MaintPrice ?? DBNull.Value;
        //    cmd.Parameters.Add("@MaintPIC", SqlDbType.NVarChar, 50).Value = (object?)maintenance.MaintPIC ?? DBNull.Value;
        //    cmd.Parameters.Add("@MaintDescription", SqlDbType.NVarChar, 255).Value = (object?)maintenance.MaintDescription ?? DBNull.Value;
        //    cmd.Parameters.Add("@MaintNote", SqlDbType.NVarChar, 255).Value = (object?)maintenance.MaintNote ?? DBNull.Value;

        //    await using var cmd2 = new SqlCommand(sqlUpdateEquipment, con);


        //    cmd2.Parameters.Add("@EQID", SqlDbType.Int).Value = ID;
        //    cmd2.Parameters.Add("@LastMaintenanceDate", SqlDbType.DateTime).Value = (object?)maintenance.MaintDate ?? DBNull.Value;

        //    await con.OpenAsync();
        //    var result = await cmd.ExecuteNonQueryAsync();
        //    return result > 0;
        //}
        private class MaintenanceSparePartDtoHelper
        {
            public long MTID { get; set; }
            public int SPID { get; set; }
            public int Qty { get; set; }
            public string? PartCode { get; set; }
            public string? PartName { get; set; }
            public string? Unit { get; set; }
        }
    }
}
