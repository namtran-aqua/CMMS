using CMMS.Shared.Equipment;

using CMMS.Server.Services.DepartmentService;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _service;
    public DepartmentController(IDepartmentService service)
    {
        _service = service;
    }
    [HttpGet("get-all")]
    public async Task<List<DepartmentDto>> GetAll()
    {
        return await _service.GetDepartmentsAsync();
    }
    [HttpGet("departments")]
    public async Task<List<DepartmentDto>> GetDepartments()
    {
        Console.WriteLine("API HIT ✅");  // 👈 thêm dòng này
        return await _service.GetDepartmentsAsync();
    }
}
