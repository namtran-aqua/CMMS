using CMMS.Shared.Dtos.DashBoards;
using Microsoft.AspNetCore.Components;

namespace CMMS.Client.Components.DashBoards.DashBoardFooters
{
    public partial class DashBoardFooter
    {
        [Parameter] public List<DashBoarDto> DashBoardData { get; set; } = new();
        private List<DashBoarDto> DueSoon { get; set; } = new();
        private List<DashBoarDto> OverDue { get; set; } = new();

        protected override void OnParametersSet()
        {
            if (DashBoardData == null) return;

            var currentDate = DateTime.Today;

            DueSoon = DashBoardData.Where(x =>
            {
                if (x.IsActive == true && x.LastMaintenanceDate.HasValue && x.MaintenanceCircleTime.HasValue)
                {
                    var nextDate = x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value);
                    var diffDays = (nextDate - currentDate).TotalDays;
                    return diffDays >= 0 && diffDays <= 7;
                }
                return false;
            })
            .OrderBy(x => x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value))
            .Take(10)
            .ToList();

            OverDue = DashBoardData.Where(x =>
            {
                if (x.IsActive == true && x.LastMaintenanceDate.HasValue && x.MaintenanceCircleTime.HasValue)
                {
                    var nextDate = x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value);
                    return nextDate < currentDate;
                }
                return false;
            })
            .OrderBy(x => x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value))
            .Take(10)
            .ToList();
        }
    }
}
