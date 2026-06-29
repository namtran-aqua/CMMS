using CMMS.Shared.Dtos.Maintenance;
using CMMS.Shared.Dtos.User;

namespace CMMS.Server.Services.MaintenanceService
{
    public interface IMaintenanceService
    {
        Task<List<MaintenanceDto>> GetAllAsync();
        Task<bool> CreatedAsync(MaintenanceDto maintenance, int eqid, UserDto currentUser);
    }
}
