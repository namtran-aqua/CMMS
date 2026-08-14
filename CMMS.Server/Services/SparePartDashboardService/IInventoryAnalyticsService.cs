using CMMS.Shared.Dtos.SpareParts.Dashboard;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartDashboardService
{
    public interface IInventoryAnalyticsService
    {
        Task<DashboardDto> GetAdvancedDashboardAsync(DashboardFilterDto filter);
    }
}
