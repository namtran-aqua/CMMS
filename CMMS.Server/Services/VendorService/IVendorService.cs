using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;

namespace CMMS.Server.Services.VendorService
{
    public interface IVendorService
    {
        Task<List<VendorDto>> GetVendorsAsync();
        Task<VendorDto> GetVendorAsync(int id);
        Task<ApiResponse> CreateVendorAsync(VendorDto vendor);
        Task<ApiResponse> UpdateVendorAsync(VendorDto vendor);
        Task<ApiResponse> DeleteVendorAsync(int id);
    }
}
