using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;

namespace CMMS.Server.Services.FactoryService
{
    public interface IFactoryService
    {
        Task<List<FactoryDto>> GetFactoriesAsync();
        Task<FactoryDto> GetFactoryAsync(int id);
        Task<ApiResponse> CreateFactoryAsync(FactoryDto factory);
        Task<ApiResponse> UpdateFactoryAsync(FactoryDto factory);
        Task<ApiResponse> DeleteFactoryAsync(int id);
    }
}
