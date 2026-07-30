using CMMS.Shared.Dtos.Equipment;
using CMMS.Server.Services.LocationService;
using Microsoft.AspNetCore.Mvc;
using CMMS.Shared.Dtos.Common;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class LocationController : ControllerBase
{
    private readonly ILocationService _service;
    public LocationController(ILocationService service)
    {
        _service = service;
    }

    [HttpGet("get-all")]
    public async Task<List<LocationDto>> GetAll()
    {
        return await _service.GetLocationsAsync();
    }

    [HttpGet("locations")]
    public async Task<List<LocationDto>> GetLocations()
    {
        return await _service.GetLocationsAsync();
    }

    [HttpGet("{id}")]
    public async Task<LocationDto> GetLocation(int id)
    {
        return await _service.GetLocationAsync(id);
    }

    [HttpPost]
    public async Task<ApiResponse> CreateLocation([FromBody] LocationDto location)
    {
        return await _service.CreateLocationAsync(location);
    }

    [HttpPut]
    public async Task<ApiResponse> UpdateLocation([FromBody] LocationDto location)
    {
        return await _service.UpdateLocationAsync(location);
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse> DeleteLocation(int id)
    {
        return await _service.DeleteLocationAsync(id);
    }
}