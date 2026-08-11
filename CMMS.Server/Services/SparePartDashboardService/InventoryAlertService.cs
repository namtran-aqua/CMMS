using CMMS.Server.Services.SparePartDashboardService.Rules;
using CMMS.Shared.Dtos.SpareParts.Dashboard;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartDashboardService
{
    public class InventoryAlertService : IInventoryAlertService
    {
        private readonly CMMS.Data.Connection.ISqlConnectionFactory _connectionFactory;
        private readonly IAlertRule _lowStockRule;
        private readonly IAlertRule _zeroStockRule;

        public InventoryAlertService(CMMS.Data.Connection.ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _lowStockRule = new LowStockRule();
            _zeroStockRule = new ZeroStockRule();
        }

        public async Task<AlertDto> GetAlertsAsync(DashboardFilterDto filter)
        {
            var lowStockCount = await _lowStockRule.EvaluateAsync(filter, _connectionFactory);
            var zeroStockCount = await _zeroStockRule.EvaluateAsync(filter, _connectionFactory);

            return new AlertDto
            {
                Warning = lowStockCount,
                Critical = zeroStockCount,
                Info = 0,    
                Normal = 0  
            };
        }
    }
}
