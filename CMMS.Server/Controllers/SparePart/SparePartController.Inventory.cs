using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CMMS.Server.Controllers.SparePart
{
    public partial class SparePartController : ControllerBase
    {
        [HttpGet("get-all")]
        public async Task<List<SparePartDto>> GetAll([FromQuery] int? factoryId = null) => await _service.GetAllAsync(factoryId);

        [HttpGet("get-paged")]
        public async Task<ActionResult<SparePartPagedResultDto>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchText = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? stockStatus = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] int? factoryId = null,
            [FromQuery] string? partCode = null,
            [FromQuery] string? partName = null,
            [FromQuery] int? supplierId = null)
        {
            return Ok(await _service.GetPagedAsync(page, pageSize, searchText, categoryId, stockStatus, sortBy, factoryId, partCode, partName, supplierId));
        }

        [HttpDelete("delete/{spid}")]
        public async Task<IActionResult> Delete(int spid)
        {
            var success = await _service.DeleteAsync(spid, await GetCurrentUserAsync());
            return success ? Ok(new { message = "Xóa thành công" }) : NotFound();
        }

        [HttpPost("adjust-stock")]
        public async Task<IActionResult> AdjustStock(AdjustStockRequestDto request)
        {
            try
            {
                var success = await _service.AdjustStockAsync(request, await GetCurrentUserAsync());
                return success ? Ok(new { message = "Cập nhật tồn kho thành công" }) : StatusCode(500, "Thất bại");
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("export-maintenance")]
        public async Task<IActionResult> ExportForMaintenance(MaintenanceExportRequestDto request)
        {
            try
            {
                var refCode = await _service.ExportForMaintenanceAsync(request, await GetCurrentUserAsync());
                return Ok(new { message = "Xuất bảo trì thành công", refCode });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("adjust-orders-all")]
        public async Task<ActionResult<List<AdjustOrderDto>>> GetAdjustOrders([FromQuery] int? factoryId = null)
        {
            return Ok(await _service.GetAdjustOrdersAsync(factoryId));
        }

        [HttpGet("adjust-order/{id}")]
        public async Task<ActionResult<AdjustOrderDto>> GetAdjustOrderById(int id)
        {
            var order = await _service.GetAdjustOrderByIdAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost("create-adjust-order")]
        public async Task<IActionResult> CreateAdjustOrder(CreateAdjustOrderDto request)
        {
            try
            {
                var result = await _service.CreateAdjustOrderAsync(request, await GetCurrentUserAsync());
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
