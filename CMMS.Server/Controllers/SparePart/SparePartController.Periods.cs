using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CMMS.Server.Controllers.SparePart
{
    public partial class SparePartController : ControllerBase
    {
        [HttpPost("close-period")]
        public async Task<IActionResult> ClosePeriod([FromQuery] int year, [FromQuery] int month)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                var success = await _service.ClosePeriodAsync(year, month, user);
                return success ? Ok(new { message = "Chốt sổ thành công" }) : BadRequest("Không thể chốt sổ");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("monthly-periods")]
        public async Task<ActionResult<PagedResultDto<SparePartMonthlyPeriodDto>>> GetMonthlyPeriods(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? year = null,
            [FromQuery] int? month = null,
            [FromQuery] int? factoryId = null)
        {
            return Ok(await _service.GetMonthlyPeriodsPagedAsync(page, pageSize, year, month, factoryId));
        }

        [HttpGet("monthly-periods-all")]
        public async Task<ActionResult<List<SparePartMonthlyPeriodDto>>> GetMonthlyPeriodsAll([FromQuery] int? factoryId = null)
        {
            return Ok(await _service.GetMonthlyPeriodsAllAsync(factoryId));
        }
    }
}
