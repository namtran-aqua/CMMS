using CMMS.Data.Connection;
using CMMS.Server.Services.Barcode;
using CMMS.Server.Services.UserService;
using CMMS.Shared.Dtos.Barcode;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMMS.Server.Controllers.QRCode
{
    [Route("api/qr")]
    [ApiController]
    public class QRCodeController : ControllerBase
    {
        private readonly IBarcodeIdService _barcodeIdService;
        private readonly IQRCodeService _qrCodeService;
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IUserService _userService;

        public QRCodeController(IBarcodeIdService barcodeIdService, IQRCodeService qrCodeService, ISqlConnectionFactory connectionFactory, IUserService userService)
        {
            _barcodeIdService = barcodeIdService;
            _qrCodeService = qrCodeService;
            _connectionFactory = connectionFactory;
            _userService = userService;
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItems([FromQuery] string type = "All", [FromQuery] string status = "All", [FromQuery] string search = "")
        {
            var results = new List<QRCodeItemDto>();
            using var connection = _connectionFactory.CreateConnection();

            bool fetchEquip = type == "All" || type == "Equipment";
            bool fetchSparePart = type == "All" || type == "SparePart";

            string statusFilterEq = status == "Generated" ? "AND EquipmentBarcode IS NOT NULL" : (status == "NotGenerated" ? "AND EquipmentBarcode IS NULL" : "");
            
            string searchLower = $"%{search?.ToLower() ?? ""}%";

            if (fetchEquip)
            {
                var sql = $@"SELECT EQID as Id, 'Equipment' as EntityType, EquipmentBarcode as BarcodeId, EquipmentCode as Code, EquipmentName as Name, EquipmentSerial as Serial, 'Active' as Status 
                             FROM Tbl_EquipmentInfo 
                             WHERE IsActive = 1 {statusFilterEq} 
                             AND (LOWER(EquipmentCode) LIKE @Search OR LOWER(EquipmentName) LIKE @Search OR LOWER(EquipmentSerial) LIKE @Search)";
                var eqs = await connection.QueryAsync<QRCodeItemDto>(sql, new { Search = searchLower });
                results.AddRange(eqs);
            }

            if (fetchSparePart)
            {
                string statusFilterSp = status == "Generated" ? "AND SparePartBarcode IS NOT NULL" : (status == "NotGenerated" ? "AND SparePartBarcode IS NULL" : "");
                var sqlMaster = $@"SELECT SPID as Id, 'SparePart' as EntityType, SparePartBarcode as BarcodeId, PartCode as Code, PartName as Name, '' as Serial, 'Active' as Status 
                             FROM Tbl_SparePart 
                             WHERE IsCoded = 0 {statusFilterSp}
                             AND (LOWER(PartCode) LIKE @Search OR LOWER(PartName) LIKE @Search)";
                var spsMaster = await connection.QueryAsync<QRCodeItemDto>(sqlMaster, new { Search = searchLower });
                results.AddRange(spsMaster);

                string statusFilterSpItem = status == "Generated" ? "AND i.SparePartBarcode IS NOT NULL" : (status == "NotGenerated" ? "AND i.SparePartBarcode IS NULL" : "");
                var sqlItem = $@"SELECT i.ItemID as Id, 'SparePart' as EntityType, i.SparePartBarcode as BarcodeId, p.PartCode as Code, p.PartName as Name, i.SerialCode as Serial, i.Status as Status 
                             FROM Tbl_SparePartItem i
                             JOIN Tbl_SparePart p ON p.SPID = i.SPID
                             WHERE p.IsCoded = 1 {statusFilterSpItem}
                             AND (LOWER(p.PartCode) LIKE @Search OR LOWER(p.PartName) LIKE @Search OR LOWER(i.SerialCode) LIKE @Search)";
                var spsItem = await connection.QueryAsync<QRCodeItemDto>(sqlItem, new { Search = searchLower });
                results.AddRange(spsItem);
            }

            return Ok(results.OrderBy(x => x.EntityType).ThenBy(x => x.Name).ToList());
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateBarcodes([FromBody] GenerateBarcodeRequestDto request)
        {
            if (request?.Items == null || !request.Items.Any())
                return BadRequest("No items selected.");

            int success = 0;
            int skipped = 0;

            using var connection = _connectionFactory.CreateConnection();
            if (connection is System.Data.Common.DbConnection dbConnection)
            {
                await dbConnection.OpenAsync();
            }
            else
            {
                connection.Open();
            }

            string deptCode = "MNT";
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var currentUser = await _userService.GetCurrentUserAsync(userId);
                if (!string.IsNullOrWhiteSpace(currentUser?.DeptCode))
                {
                    deptCode = currentUser.DeptCode;
                }
            }

            foreach (var item in request.Items)
            {
                // Verify if it already has a barcode
                string currentBarcode = null;
                if (item.EntityType == "Equipment")
                    currentBarcode = await connection.QuerySingleOrDefaultAsync<string>("SELECT EquipmentBarcode FROM Tbl_EquipmentInfo WHERE EQID = @Id", new { item.Id });
                else if (item.EntityType == "SparePart")
                {
                    if (string.IsNullOrEmpty(item.Serial))
                        currentBarcode = await connection.QuerySingleOrDefaultAsync<string>("SELECT SparePartBarcode FROM Tbl_SparePart WHERE SPID = @Id", new { item.Id });
                    else
                        currentBarcode = await connection.QuerySingleOrDefaultAsync<string>("SELECT SparePartBarcode FROM Tbl_SparePartItem WHERE ItemID = @Id", new { item.Id });
                }

                if (!string.IsNullOrEmpty(currentBarcode))
                {
                    skipped++;
                    continue; // Already has barcode
                }

                // Generate new barcode
                string newBarcode = null;
                if (item.EntityType == "Equipment")
                {
                    newBarcode = await _barcodeIdService.GenerateEquipmentBarcodeIdAsync(deptCode);
                    await connection.ExecuteAsync("UPDATE Tbl_EquipmentInfo SET EquipmentBarcode = @Barcode WHERE EQID = @Id", new { Barcode = newBarcode, item.Id });
                }
                else if (item.EntityType == "SparePart")
                {
                    newBarcode = await _barcodeIdService.GenerateSparePartBarcodeIdAsync(deptCode);
                    if (string.IsNullOrEmpty(item.Serial))
                        await connection.ExecuteAsync("UPDATE Tbl_SparePart SET SparePartBarcode = @Barcode WHERE SPID = @Id", new { Barcode = newBarcode, item.Id });
                    else
                        await connection.ExecuteAsync("UPDATE Tbl_SparePartItem SET SparePartBarcode = @Barcode WHERE ItemID = @Id", new { Barcode = newBarcode, item.Id });
                }

                if (newBarcode != null)
                {
                    success++;
                }
            }

            return Ok(new { Success = success, Skipped = skipped, Total = request.Items.Count });
        }

        [HttpPost("export-pdf")]
        public IActionResult ExportPdf([FromBody] ExportPdfRequestDto request)
        {
            if (request?.Items == null || !request.Items.Any())
                return BadRequest("No items selected.");

            var labels = request.Items
                .Where(x => !string.IsNullOrEmpty(x.BarcodeId))
                .Select(x => new LabelInfo
                {
                    BarcodeId = x.BarcodeId,
                    EntityName = x.Name,
                    AdditionalInfo = string.IsNullOrEmpty(x.Serial) ? x.Code : $"{x.Code} | SN: {x.Serial}"
                }).ToList();

            if (!labels.Any())
                return BadRequest("Selected items do not have Barcode IDs.");

            string fileName = labels.Count == 1 ? $"{labels[0].BarcodeId}.pdf" : $"{labels[0].BarcodeId}_and_{labels.Count - 1}_others.pdf";
            var pdfBytes = _qrCodeService.GeneratePdfLabels(labels);
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet("image/{barcodeId}")]
        public IActionResult GetQrCodeImage(string barcodeId)
        {
            if (string.IsNullOrEmpty(barcodeId))
                return BadRequest("Barcode ID is required");

            var imageBytes = _qrCodeService.GenerateQrCode(barcodeId);
            return File(imageBytes, "image/png");
        }
    }
}


