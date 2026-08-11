using CMMS.Shared.Dtos.SpareParts.Dashboard;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartDashboardService
{
    public interface IInventoryAlertService
    {
        Task<AlertDto> GetAlertsAsync(DashboardFilterDto filter);
    }
}
