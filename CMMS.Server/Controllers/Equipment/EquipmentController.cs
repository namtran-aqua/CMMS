using CMMS.Server.Services.EquipmentService;
using CMMS.Server.Services.DailyJobService;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CMMS.Server.Services.UserService;
using System.IO;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentService _service;
    private readonly IDailyJobService _statusService;
    private readonly IUserService _userService;

    public EquipmentController(IEquipmentService service, IDailyJobService status, IUserService userService)
    {
        _service = service;
        _statusService = status;
        _userService = userService;
    }

    [HttpGet("get-all")]
    public async Task<List<EquipmentDto>> GetAll([FromQuery] int? factoryId = null)
    {
        return await _service.GetAllAsync(factoryId);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File không hợp lệ hoặc rỗng.");

        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Không xác định được người dùng.");
            
            var currentUser = await _userService.GetCurrentUserAsync(userId);
            if (currentUser == null) return Unauthorized("Không tìm thấy thông tin người dùng.");

            using var stream = file.OpenReadStream();
            var result = await _service.ImportEquipmentsAsync(stream, file.FileName, currentUser);
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
        var csv = "EquipmentCode,EquipmentName,EquipmentModel,EquipmentSerial,EquipmentDescription,EquipmentNote,LocCode,DeptCode,BuyDate,BuyPrice,BuyCurrency,MaintenanceCircleTime,ContactNo,SAPCode,PIC\n" +
                  "EQ-001,Máy phay CNC Haas VF-2,VF-2,1029384,Máy phay đứng tốc độ cao,Hoạt động tốt,LOC-A,Maint-Dept,2025-01-15,1200000000,VND,180,0901234567,1001,Nguyễn Văn A\n" +
                  "EQ-002,Máy nén khí Atlas Copco,GA37,GA992837,Máy nén khí trục vít,Hoạt động 24/7,LOC-B,Prod-Dept,2024-06-20,350000000,VND,90,0907654321,1002,Trần Văn B";
        
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv; charset=utf-8", "Equipment_Import_Template.csv");
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(EquipmentDto equipment)
    {
        if (equipment == null)
            return BadRequest("Invalid data");
        var success = await _service.CreatedAsync(equipment);
        if (success)
            return Ok(new {message = " Tạo mới thành công"});
        return StatusCode (500, "Tạo mới thất bại");
    }
     [HttpPut("update")]
     public async Task<IActionResult> Update(EquipmentDto equipment)
    {             
        if (equipment == null)
                return BadRequest("Invalid data");
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User ID claim is missing or invalid.");
        try
        {
            var currentUser = await _userService.GetCurrentUserAsync(userId);
            var success = await _service.UpdateAsync(equipment, currentUser);
            
            if (success)
                return Ok(new {message = "Cập nhật thành công"});
            return StatusCode(500, "Cập nhật thất bại");

        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        //var success = await _service.UpdateAsync(equipment);
        //if (success)
        //    return Ok(new {message = "Cập nhật thành công"});
        //return NotFound("Không tìm thấy Equipment");
    }
    [HttpDelete("delete/{eqId}")]
    [HttpDelete("delete/{eqId}")]
    public async Task<IActionResult> Delete(int eqId)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User ID claim is missing or invalid.");

        try
        {
            var currentUser = await _userService.GetCurrentUserAsync(userId);
            var success = await _service.DeleteAsync(eqId, currentUser);

            if (success)
                return Ok(new { message = "Xóa thành công" });

            return StatusCode(500, "Xóa thất bại");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
        //var success = await _service.DeleteAsync(eqId);
        //if (success)
        //    return Ok(new {message = "Xóa thành công"});
        //return NotFound("Không tìm thấy Equipment");
    }
    [HttpPost("update-status")]
    public async Task<IActionResult> UpdateStatus()
    {
        await _statusService.UpdateStatusAsync();
        return Ok();
    }
    //[HttpPost("request/{eqId}")]
    //public async Task<IActionResult> RequestScrap(int eqId)
    //{
    //    var result = await _service.RequestScrapAsync(eqId);

    //    if (!result.Success)
    //    {
    //        return BadRequest(result.Message);
    //    }

    //    return Ok(new
    //    {
    //        message = result.Message
    //    });
    //}
}