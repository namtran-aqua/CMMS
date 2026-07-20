using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMMS.Server.Controllers.SparePart
{
    public partial class SparePartController : ControllerBase
    {
        [HttpGet("history-paged")]
        public async Task<ActionResult<PagedResultDto<SparePartTransactionDto>>> GetHistoryPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchText = null,
            [FromQuery] string? typeFilter = null,
            [FromQuery] int? factoryId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            return Ok(await _service.GetTransactionHistoryPagedAsync(page, pageSize, searchText, typeFilter, factoryId, fromDate, toDate));
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ hoặc rỗng.");

            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized("Không tìm thấy thông tin người dùng.");

                using var stream = file.OpenReadStream();
                var result = await _service.ImportSparePartsAsync(stream, file.FileName, currentUser);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi import: {ex.Message}");
            }
        }

        [HttpGet("import-template")]
        public IActionResult DownloadTemplate()
        {
            var csv = "PartCode,PartName,Unit,Price,Inventory,MinStock,CategoryName,SupplierName,LocName,DeptCode,Note\n" +
                      "SP-001,Cảm biến tiệm cận M18,Cái,150000,20,5,Cảm biến,Nhà cung cấp Omron,Khu vực A,Maint-Dept,Ghi chú mẫu\n" +
                      "SP-002,Băng tải cao su B500,Mét,450000,5,2,Băng tải,Nhà cung cấp Habasit,Khu vực B,Prod-Dept,Ghi chú mẫu 2";
            
            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray();
            return File(bytes, "text/csv; charset=utf-8", "SparePart_Import_Template.csv");
        }

        [HttpGet("history")]
        public async Task<List<SparePartTransactionDto>> GetHistory([FromQuery] int? factoryId = null) => await _service.GetTransactionHistoryAsync(factoryId);

        [HttpPost("import-order")]
        public async Task<IActionResult> CreateImportOrder(ImportOrderDto dto)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                var result = await _service.CreateImportOrderAsync(dto, user);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("import-order/{importId}/status")]
        public async Task<IActionResult> UpdateImportOrderStatus(int importId, [FromQuery] string status)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                var result = await _service.UpdateImportOrderStatusAsync(importId, status, user);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("import-orders")]
        public async Task<ActionResult<PagedResultDto<ImportOrderDto>>> GetImportOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? importCode = null,
            [FromQuery] string? po = null,
            [FromQuery] int? vendorId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? factoryId = null,
            [FromQuery] Guid? createdBy = null)
        {
            return Ok(await _service.GetImportOrdersPagedAsync(page, pageSize, importCode, po, vendorId, fromDate, toDate, factoryId, createdBy));
        }

        [HttpGet("import-order/{importId}")]
        public async Task<ActionResult<ImportOrderDto>> GetImportOrderDetail(int importId)
        {
            var result = await _service.GetImportOrderDetailAsync(importId);
            if (result == null) return NotFound("Không tìm thấy lệnh nhập.");
            return Ok(result);
        }

        [HttpPost("export-order")]
        public async Task<IActionResult> CreateExportOrder(ExportOrderDto dto)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                var result = await _service.CreateExportOrderAsync(dto, user);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("export-order/{exportId}/status")]
        public async Task<IActionResult> UpdateExportOrderStatus(int exportId, [FromQuery] string status)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                var result = await _service.UpdateExportOrderStatusAsync(exportId, status, user);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("export-orders")]
        public async Task<ActionResult<PagedResultDto<ExportOrderDto>>> GetExportOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? exportCode = null,
            [FromQuery] int? movementTypeId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? factoryId = null,
            [FromQuery] Guid? createdBy = null)
        {
            return Ok(await _service.GetExportOrdersPagedAsync(page, pageSize, exportCode, movementTypeId, fromDate, toDate, factoryId, createdBy));
        }

        [HttpGet("export-order/{exportId}")]
        public async Task<ActionResult<ExportOrderDto>> GetExportOrderDetail(int exportId)
        {
            var result = await _service.GetExportOrderDetailAsync(exportId);
            if (result == null) return NotFound("Không tìm thấy lệnh xuất.");
            return Ok(result);
        }

        [HttpGet("movement-types")]
        public async Task<ActionResult<List<MovementTypeDto>>> GetMovementTypes([FromQuery] int? factoryId = null)
        {
            return Ok(await _service.GetMovementTypesAsync(factoryId));
        }

        [HttpGet("coded-items")]
        public async Task<ActionResult<PagedResultDto<SparePartItemDto>>> GetCodedSpareParts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? serialCode = null,
            [FromQuery] string? partCode = null,
            [FromQuery] string? partName = null,
            [FromQuery] string? status = null,
            [FromQuery] int? factoryId = null)
        {
            return Ok(await _service.GetCodedSparePartsPagedAsync(page, pageSize, serialCode, partCode, partName, status, factoryId));
        }

        [HttpGet("item-history/{spid}")]
        public async Task<ActionResult<List<SparePartTransactionDto>>> GetSparePartMovementHistory(int spid)
        {
            return Ok(await _service.GetSparePartMovementHistoryAsync(spid));
        }

        [HttpGet("import-orders-all")]
        public async Task<ActionResult<List<ImportOrderDto>>> GetImportOrdersAll([FromQuery] int? factoryId = null)
        {
            return Ok(await _service.GetImportOrdersAllAsync(factoryId));
        }

        [HttpGet("export-orders-all")]
        public async Task<ActionResult<List<ExportOrderDto>>> GetExportOrdersAll([FromQuery] int? factoryId = null)
        {
            return Ok(await _service.GetExportOrdersAllAsync(factoryId));
        }

        [HttpGet("coded-items-all")]
        public async Task<ActionResult<List<SparePartItemDto>>> GetCodedSparePartsAll([FromQuery] int? factoryId = null)
        {
            return Ok(await _service.GetCodedSparePartsAllAsync(factoryId));
        }
    }
}
