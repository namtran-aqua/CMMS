using CMMS.Shared.Dtos.Equipment;

namespace CMMS.Server.Services.VendorService
{
    public interface IVendorService
    {
        Task<List<VendorDto>> GetVendorsAsync();
    }
}
