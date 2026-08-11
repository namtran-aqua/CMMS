using CMMS.Shared.Dtos.SpareParts.Dashboard;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartDashboardService
{
    public interface ISparePartDashboardService
    {
        Task<DashboardDto> GetDashboardAsync(DashboardFilterDto filter);
    }
}
