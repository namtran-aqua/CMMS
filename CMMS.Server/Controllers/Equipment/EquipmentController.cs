using CMMS.Server.Services.EquipmentService;
using CMMS.Shared.EquipmentDto;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentService _service;

    public EquipmentController(IEquipmentService service)
    {
        _service = service;
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
    [HttpDelete("delete/{equipmentCode}")]
    public async Task<IActionResult> Delete(int equipmentCode)
    {
        var success = await _service.DeleteAsync(equipmentCode);
        if (success)
            return Ok(new {message = "Xóa thành công"});
        return NotFound("Không tìm thấy Equipment");
    }
}