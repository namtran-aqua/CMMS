namespace CMMS.Server.Services.SparePartDashboardService
{
    public class DashboardSettings
    {
        public int TopConsumedLimit { get; set; } = 10;
        public int TrendMonth { get; set; } = 12;
        public int CacheDurationSeconds { get; set; } = 60;
        
        // Alert thresholds
        public int SlowMovingDays { get; set; } = 180;
        public decimal LowStockPercent { get; set; } = 20m;
        public decimal CriticalDays { get; set; } = 7m;
    }
}
