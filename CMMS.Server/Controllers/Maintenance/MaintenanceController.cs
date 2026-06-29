using CMMS.Server.Services.MaintenanceService;
using CMMS.Server.Services.UserService;
using CMMS.Shared.Dtos.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _service;
    private readonly IUserService _userService;
    public MaintenanceController(IMaintenanceService service, IUserService userService)
    {
        _service = service;
        _userService = userService;
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
        foreach (var claim in User.Claims)
            Console.WriteLine($"{claim.Type} = {claim.Value}");
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Không xác định được người dùng.");
   
        try
        {
            var currentUser = await _userService.GetCurrentUserAsync(userId);
            var success = await _service.CreatedAsync(maintenance, eqid, currentUser);

            if (success)
                return Ok(new { message = "Thành công" });

            return StatusCode(500, "Thất bại");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
    }
}