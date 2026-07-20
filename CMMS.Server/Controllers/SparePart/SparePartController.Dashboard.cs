using CMMS.Shared.Dtos.SpareParts;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CMMS.Server.Controllers.SparePart
{
    public partial class SparePartController : ControllerBase
    {
        [HttpGet("dashboard")]
        public async Task<ActionResult<SparePartDashboardDto>> GetDashboard([FromQuery] int? factoryId = null)
        {
            return Ok(await _service.GetSparePartDashboardAsync(factoryId));
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel([FromQuery] int? factoryId = null)
        {
            try
            {
                var bytes = await _service.ExportInventoryToExcelAsync(factoryId);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SparePart_Inventory_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi xuất excel: {ex.Message}");
            }
        }
    }
}
