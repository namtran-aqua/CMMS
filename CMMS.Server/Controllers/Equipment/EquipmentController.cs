using CMMS.Server.Services.EquipmentService;
using CMMS.Server.Services.DailyJobService;
using CMMS.Shared.Dtos.Equipment;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentService _service;
    private readonly IDailyJobService _statusService;

    public EquipmentController(IEquipmentService service, IDailyJobService status)
    {
        _service = service;
        _statusService = status;
    }

    [HttpGet("get-all")]
    public async Task<List<EquipmentDto>> GetAll()
    {
        return await _service.GetAllAsync();
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
        var success = await _service.UpdateAsync(equipment);
        if (success)
            return Ok(new {message = "Cập nhật thành công"});
        return NotFound("Không tìm thấy Equipment");
    }
    [HttpDelete("delete/{eqId}")]
    public async Task<IActionResult> Delete(int eqId)
    {
        var success = await _service.DeleteAsync(eqId);
        if (success)
            return Ok(new {message = "Xóa thành công"});
        return NotFound("Không tìm thấy Equipment");
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