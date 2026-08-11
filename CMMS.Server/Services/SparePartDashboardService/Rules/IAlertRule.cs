using CMMS.Shared.Dtos.SpareParts.Dashboard;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartDashboardService.Rules
{
    public interface IAlertRule
    {
        Task<int> EvaluateAsync(DashboardFilterDto filter, CMMS.Data.Connection.ISqlConnectionFactory connectionFactory);
    }
}
