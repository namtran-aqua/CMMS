using CMMS.Server.Services.StatusUsingService;
using Microsoft.AspNetCore.Mvc;
using CMMS.Shared.Dtos.Equipment;

[ApiController]
[Route("api/[controller]")]
public class StatusUsingController : ControllerBase
{
    private readonly IStatusUsingService _service;
    public StatusUsingController(IStatusUsingService service)
    {
        _service = service;
    }
    [HttpGet("get-all")]
    public async Task<List<StatusUsingDto>> GetAll()
    {
        return await _service.GetStatusUsingAsync();
    }
    [HttpGet("statususing")]
    public async Task<List<StatusUsingDto>> GetVendors()
    {
        Console.WriteLine("API HIT ✅");  // 👈 thêm dòng này
        return await _service.GetStatusUsingAsync();
    }
}
