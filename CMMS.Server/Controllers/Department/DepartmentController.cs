using CMMS.Server.Services.DepartmentService;
using Microsoft.AspNetCore.Mvc;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _service;
    public DepartmentController(IDepartmentService service)
    {
        _service = service;
    }

    [HttpGet("get-all")]
    [AllowAnonymous]
    public async Task<List<DepartmentDto>> GetAll()
    {
        return await _service.GetDepartmentsAsync();
    }

    [HttpGet("departments")]
    [AllowAnonymous]
    public async Task<List<DepartmentDto>> GetDepartments()
    {
        return await _service.GetDepartmentsAsync();
    }

    [HttpGet("factory/{factoryId}")]
    [AllowAnonymous]
    public async Task<List<DepartmentDto>> GetDepartmentsByFactory(int factoryId)
    {
        return await _service.GetDepartmentsByFactoryAsync(factoryId);
    }

    [HttpGet("{id}")]
    public async Task<DepartmentDto> GetDepartment(int id)
    {
        return await _service.GetDepartmentAsync(id);
    }

    [HttpPost]
    public async Task<ApiResponse> CreateDepartment([FromBody] DepartmentDto department)
    {
        return await _service.CreateDepartmentAsync(department);
    }

    [HttpPut]
    public async Task<ApiResponse> UpdateDepartment([FromBody] DepartmentDto department)
    {
        return await _service.UpdateDepartmentAsync(department);
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse> DeleteDepartment(int id)
    {
        return await _service.DeleteDepartmentAsync(id);
    }
}
