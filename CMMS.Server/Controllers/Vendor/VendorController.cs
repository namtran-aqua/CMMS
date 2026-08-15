using CMMS.Server.Services.VendorService;
using Microsoft.AspNetCore.Mvc;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;
using Microsoft.AspNetCore.Authorization;
using CMMS.Shared.Authorization;
using CMMS.Server.Infrastructure.Authorization;

namespace CMMS.Server.Controllers.Vendor
{
    [ApiController]
    [Route("api/[controller]")]
    [RequirePermission(Permissions.MasterDataVendorView)]
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
        return await _service.GetVendorsAsync();
    }

    [HttpGet("{id}")]
    public async Task<VendorDto> GetVendor(int id)
    {
        return await _service.GetVendorAsync(id);
    }

    [HttpPost]
    [RequirePermission(Permissions.MasterDataVendorAdd)]
    public async Task<ApiResponse> CreateVendor([FromBody] VendorDto vendor)
    {
        return await _service.CreateVendorAsync(vendor);
    }

    [HttpPut]
    [RequirePermission(Permissions.MasterDataVendorEdit)]
    public async Task<ApiResponse> UpdateVendor([FromBody] VendorDto vendor)
    {
        return await _service.UpdateVendorAsync(vendor);
    }

    [HttpDelete("{id}")]
    [RequirePermission(Permissions.MasterDataVendorDelete)]
    public async Task<ApiResponse> DeleteVendor(int id)
    {
        return await _service.DeleteVendorAsync(id);
    }
    }
}
