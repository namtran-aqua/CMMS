using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;
namespace CMMS.Server.Services.LocationService
{
    public interface ILocationService
    {
        Task<List<LocationDto>> GetLocationsAsync();
        Task<LocationDto> GetLocationAsync(int id);
        Task<ApiResponse> CreateLocationAsync(LocationDto location);
        Task<ApiResponse> UpdateLocationAsync(LocationDto location);
        Task<ApiResponse> DeleteLocationAsync(int id);
    }
}
