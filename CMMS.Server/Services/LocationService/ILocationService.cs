using CMMS.Shared.Dtos.Equipment;
namespace CMMS.Server.Services.LocationService
{
    public interface ILocationService
    {
        Task<List<LocationDto>> GetLocationsAsync();
    }
}
