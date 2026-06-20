using CMMS.Shared.Dtos.Equipment;
using CMMS.Server.Services.LocationService;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
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
        Console.WriteLine("API HIT ✅");  // 👈 thêm dòng này
        return await _service.GetLocationsAsync();
    }
}