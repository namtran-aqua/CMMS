using CMMS.Shared.Dtos.SpareParts.Dashboard;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartDashboardService
{
    public class SparePartDashboardService : ISparePartDashboardService
    {
        private readonly IInventoryAnalyticsService _analyticsService;
        private readonly IInventoryKpiService _kpiService;
        private readonly IInventoryAlertService _alertService;
        private readonly IMemoryCache _cache;

        public SparePartDashboardService(
            IInventoryAnalyticsService analyticsService,
            IInventoryKpiService kpiService,
            IInventoryAlertService alertService,
            IMemoryCache cache)
        {
            _analyticsService = analyticsService;
            _kpiService = kpiService;
            _alertService = alertService;
            _cache = cache;
        }

        public async Task<DashboardDto> GetDashboardAsync(DashboardFilterDto filter)
        {
            // Build comprehensive cache key
            string cacheKey = $"dashboard:{filter.FactoryId}:{filter.SectionId}:{filter.StartDate?.ToString("yyyyMMdd")}:{filter.EndDate?.ToString("yyyyMMdd")}:{filter.MovementType}";

            if (!_cache.TryGetValue(cacheKey, out DashboardDto dashboardData))
            {
                // Orchestrate calls to sub-services
                // We can run these in parallel for performance using Task.WhenAll if they don't share same DB context tracking issues,
                // but awaiting sequentially is safer if they share scoped DbContext.
                
                var summary = await _analyticsService.GetSummaryAsync(filter);
                var kpis = await _kpiService.GetKpisAsync(filter);
                var alerts = await _alertService.GetAlertsAsync(filter);
                var trends = await _analyticsService.GetTrendsAsync(filter);
                var topConsumed = await _analyticsService.GetTopConsumedAsync(filter);
                var recentMovements = await _analyticsService.GetRecentMovementsAsync(filter);
                var categoryValues = await _analyticsService.GetCategoryValuesAsync(filter);

                dashboardData = new DashboardDto
                {
                    Summary = summary,
                    Kpi = kpis,
                    Alerts = alerts,
                    Trends = trends,
                    TopConsumed = topConsumed,
                    RecentMovements = recentMovements,
                    CategoryValues = categoryValues
                };

                // Cache the aggregated dashboard result
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = System.TimeSpan.FromSeconds(60) // Configured via DashboardSettings
                };
                _cache.Set(cacheKey, dashboardData, cacheOptions);
            }

            return dashboardData;
        }
    }
}
