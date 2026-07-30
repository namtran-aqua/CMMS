using CMMS.Server.Services.FactoryService;
using Microsoft.AspNetCore.Mvc;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class FactoryController : ControllerBase
{
    private readonly IFactoryService _service;
    public FactoryController(IFactoryService service)
    {
        _service = service;
    }

    [HttpGet("get-all")]
    [AllowAnonymous]
    public async Task<List<FactoryDto>> GetAll()
    {
        return await _service.GetFactoriesAsync();
    }

    [HttpGet("factories")]
    [AllowAnonymous]
    public async Task<List<FactoryDto>> GetFactories()
    {
        return await _service.GetFactoriesAsync();
    }

    [HttpGet("{id}")]
    public async Task<FactoryDto> GetFactory(int id)
    {
        return await _service.GetFactoryAsync(id);
    }

    [HttpPost]
    public async Task<ApiResponse> CreateFactory([FromBody] FactoryDto factory)
    {
        return await _service.CreateFactoryAsync(factory);
    }

    [HttpPut]
    public async Task<ApiResponse> UpdateFactory([FromBody] FactoryDto factory)
    {
        return await _service.UpdateFactoryAsync(factory);
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse> DeleteFactory(int id)
    {
        return await _service.DeleteFactoryAsync(id);
    }
}
