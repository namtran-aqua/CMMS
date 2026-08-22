using CMMS.Data.Connection;
using CMMS.Server.Services.Barcode;
using CMMS.Shared.Dtos.Barcode;
using Dapper;
using Microsoft.AspNetCore.Mvc;
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

        public QRCodeController(IBarcodeIdService barcodeIdService, IQRCodeService qrCodeService, ISqlConnectionFactory connectionFactory)
        {
            _barcodeIdService = barcodeIdService;
            _qrCodeService = qrCodeService;
            _connectionFactory = connectionFactory;
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItems([FromQuery] string type = "All", [FromQuery] string status = "All", [FromQuery] string search = "")
        {
            var results = new List<QRCodeItemDto>();
            using var connection = _connectionFactory.CreateConnection();

            bool fetchEquip = type == "All" || type == "Equipment";
            bool fetchSparePart = type == "All" || type == "SparePart";

            string statusFilterEq = status == "Generated" ? "AND EquipmentBarcode IS NOT NULL" : (status == "NotGenerated" ? "AND EquipmentBarcode IS NULL" : "");
            string statusFilterSp = status == "Generated" ? "AND SparePartBarcode IS NOT NULL" : (status == "NotGenerated" ? "AND SparePartBarcode IS NULL" : "");

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
                var sql = $@"SELECT SPID as Id, 'SparePart' as EntityType, SparePartBarcode as BarcodeId, PartCode as Code, PartName as Name, '' as Serial, 'Active' as Status 
                             FROM Tbl_SparePart 
                             WHERE 1=1 {statusFilterSp}
                             AND (LOWER(PartCode) LIKE @Search OR LOWER(PartName) LIKE @Search)";
                var sps = await connection.QueryAsync<QRCodeItemDto>(sql, new { Search = searchLower });
                results.AddRange(sps);
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

            foreach (var item in request.Items)
            {
                // Verify if it already has a barcode
                string currentBarcode = null;
                if (item.EntityType == "Equipment")
                    currentBarcode = await connection.QuerySingleOrDefaultAsync<string>("SELECT EquipmentBarcode FROM Tbl_EquipmentInfo WHERE EQID = @Id", new { item.Id });
                else if (item.EntityType == "SparePart")
                    currentBarcode = await connection.QuerySingleOrDefaultAsync<string>("SELECT SparePartBarcode FROM Tbl_SparePart WHERE SPID = @Id", new { item.Id });

                if (!string.IsNullOrEmpty(currentBarcode))
                {
                    skipped++;
                    continue; // Already has barcode
                }

                // Generate new barcode
                string newBarcode = null;
                if (item.EntityType == "Equipment")
                {
                    newBarcode = await _barcodeIdService.GenerateEquipmentBarcodeIdAsync();
                    await connection.ExecuteAsync("UPDATE Tbl_EquipmentInfo SET EquipmentBarcode = @Barcode WHERE EQID = @Id", new { Barcode = newBarcode, item.Id });
                }
                else if (item.EntityType == "SparePart")
                {
                    newBarcode = await _barcodeIdService.GenerateSparePartBarcodeIdAsync();
                    await connection.ExecuteAsync("UPDATE Tbl_SparePart SET SparePartBarcode = @Barcode WHERE SPID = @Id", new { Barcode = newBarcode, item.Id });
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

            var pdfBytes = _qrCodeService.GeneratePdfLabels(labels);
            return File(pdfBytes, "application/pdf", "qr_labels.pdf");
        }
    }
}
