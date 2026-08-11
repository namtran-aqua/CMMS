using CMMS.Shared.Dtos.SpareParts.Dashboard;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartDashboardService
{
    public interface IInventoryAnalyticsService
    {
        Task<SummaryDto> GetSummaryAsync(DashboardFilterDto filter);
        Task<List<TrendDto>> GetTrendsAsync(DashboardFilterDto filter);
        Task<List<TopConsumedDto>> GetTopConsumedAsync(DashboardFilterDto filter);
        Task<List<RecentMovementDto>> GetRecentMovementsAsync(DashboardFilterDto filter);
        Task<List<CategoryValueDto>> GetCategoryValuesAsync(DashboardFilterDto filter);
    }
}
