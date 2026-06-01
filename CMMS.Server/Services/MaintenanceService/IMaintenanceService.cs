using CMMS.Shared.Dtos.Maintenance;

namespace CMMS.Server.Services.MaintenanceService
{
    public interface IMaintenanceService
    {
        Task<List<MaintenanceDto>> GetAllAsync();
        Task<bool> CreatedAsync(MaintenanceDto maintenance, int eqid);
    }
}
