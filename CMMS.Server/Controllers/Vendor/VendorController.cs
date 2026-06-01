using CMMS.Server.Services.VendorService;
using Microsoft.AspNetCore.Mvc;
using CMMS.Shared.Dtos.Equipment;

[ApiController]
[Route("api/[controller]")]
public class VendorController : ControllerBase
{
    private readonly IVendorService _service;
    public VendorController(IVendorService service)
    {
        _service = service;
    }
    [HttpGet("get-all")]
    public async Task<List<VendorDto>> GetAll()
    {
        return await _service.GetVendorsAsync();
    }
    [HttpGet("vendors")]
    public async Task<List<VendorDto>> GetVendors()
    {
        Console.WriteLine("API HIT ✅");  // 👈 thêm dòng này
        return await _service.GetVendorsAsync();
    }
}
