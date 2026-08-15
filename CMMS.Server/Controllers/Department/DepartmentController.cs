using CMMS.Server.Services.DepartmentService;
using Microsoft.AspNetCore.Mvc;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;
using Microsoft.AspNetCore.Authorization;
using CMMS.Shared.Authorization;
using CMMS.Server.Infrastructure.Authorization;

[ApiController]
[Route("api/[controller]")]
[RequirePermission(Permissions.MasterDataDepartmentView)]
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
    [RequirePermission(Permissions.MasterDataDepartmentAdd)]
    public async Task<ApiResponse> CreateDepartment([FromBody] DepartmentDto department)
    {
        return await _service.CreateDepartmentAsync(department);
    }

    [HttpPut]
    [RequirePermission(Permissions.MasterDataDepartmentEdit)]
    public async Task<ApiResponse> UpdateDepartment([FromBody] DepartmentDto department)
    {
        return await _service.UpdateDepartmentAsync(department);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.MasterDataDepartmentDelete)]
    public async Task<ApiResponse> DeleteDepartment(int id)
    {
        return await _service.DeleteDepartmentAsync(id);
    }
}
