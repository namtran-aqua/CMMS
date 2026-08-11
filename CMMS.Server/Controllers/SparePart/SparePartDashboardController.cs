using CMMS.Server.Services.SparePartDashboardService;
using CMMS.Shared.Dtos.SpareParts.Dashboard;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CMMS.Server.Controllers.SparePart
{
    [Route("api/[controller]")]
    [ApiController]
    public class SparePartDashboardController : ControllerBase
    {
        private readonly ISparePartDashboardService _dashboardService;

        public SparePartDashboardController(ISparePartDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("advanced-dashboard")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<ActionResult<DashboardDto>> GetAdvancedDashboard([FromQuery] DashboardFilterDto filter)
        {
            var result = await _dashboardService.GetDashboardAsync(filter);
            return Ok(result);
        }

        [HttpGet("export-excel")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> ExportExcel([FromQuery] DashboardFilterDto filter)
        {
            var dashboardData = await _dashboardService.GetDashboardAsync(filter);

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Dashboard Report");
            
            // Generate some headers
            worksheet.Cell(1, 1).Value = "Spare Part Dashboard Report";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            
            worksheet.Cell(3, 1).Value = "KPIs";
            worksheet.Cell(4, 1).Value = "Inventory Value (VND)";
            worksheet.Cell(4, 2).Value = dashboardData.Kpi.InventoryValue;
            worksheet.Cell(5, 1).Value = "Stock Accuracy (%)";
            worksheet.Cell(5, 2).Value = dashboardData.Kpi.StockAccuracy;
            
            worksheet.Cell(7, 1).Value = "Alerts";
            worksheet.Cell(8, 1).Value = "Critical Alerts";
            worksheet.Cell(8, 2).Value = dashboardData.Alerts.Critical;
            worksheet.Cell(9, 1).Value = "Warning Alerts";
            worksheet.Cell(9, 2).Value = dashboardData.Alerts.Warning;

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Dashboard_Report_{System.DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
    }
}
