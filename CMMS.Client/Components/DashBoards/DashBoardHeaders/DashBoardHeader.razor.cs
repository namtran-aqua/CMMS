using CMMS.Shared.Dtos.DashBoards;
using Microsoft.AspNetCore.Components;

namespace CMMS.Client.Components.DashBoards.DashBoardHeaders
{
    public partial class DashBoardHeader
    {
        [Parameter] public List<DashBoarDto> DashBoardData { get; set; } = new();
        private List<DashBoarDto> Normal { get; set; } = new();
        private List<DashBoarDto> Running { get; set; } = new();
        private List<DashBoarDto> DueSoon { get; set; } = new();
        private List<DashBoarDto> OverDue { get; set; } = new();

        protected override void OnParametersSet()
        {
            if (DashBoardData == null) return;

            Running = DashBoardData.Where(x => x.IsActive == true).ToList();

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
            }).ToList();

            OverDue = DashBoardData.Where(x =>
            {
                if (x.IsActive == true && x.LastMaintenanceDate.HasValue && x.MaintenanceCircleTime.HasValue)
                {
                    var nextDate = x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value);
                    return nextDate < currentDate;
                }
                return false;
            }).ToList();

            Normal = Running.Except(DueSoon).Except(OverDue).ToList();
        }
    }
}
