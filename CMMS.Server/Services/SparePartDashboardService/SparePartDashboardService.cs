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
            string cacheKey = $"dashboard:{filter.FactoryId}:{filter.SectionId}:{filter.DepartmentId}:{filter.StartDate?.ToString("yyyyMMdd")}:{filter.EndDate?.ToString("yyyyMMdd")}:{filter.MovementType}";

            if (!_cache.TryGetValue(cacheKey, out DashboardDto dashboardData))
            {
                dashboardData = await _analyticsService.GetAdvancedDashboardAsync(filter);

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
