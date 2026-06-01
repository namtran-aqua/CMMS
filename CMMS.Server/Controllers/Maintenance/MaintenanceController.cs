using CMMS.Server.Services.MaintenanceService;
using CMMS.Shared.Dtos.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _service;
    public MaintenanceController(IMaintenanceService service)
    {
        _service = service;
    }
    [HttpGet("get-all")]
    public async Task<List<MaintenanceDto>> GetAll()
    {
        return await _service.GetAllAsync();
    }
    [HttpPost("create/{eqid}")]
    public async Task<IActionResult> Create(MaintenanceDto maintenance, int eqid)
    {
        if (maintenance == null)
            return BadRequest("Invalid data");
        var success = await _service.CreatedAsync(maintenance, eqid);
        if (success)
            return Ok(new { message = "Cập nhật thành công" });
        return StatusCode(500, "Cập nhật thất bại");
    }
}