using CMMS.Shared.EquipmentDto;
using Microsoft.AspNetCore.Components;

namespace CMMS.Client.Pages.Maintenance
{
    public partial class Maintenance
    {
        [Inject] private HttpClient Http { get; set; }
        private List<EquipmentDto> _equipments = new();
        private string selectedTab = "Pending";
        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }
        //private async Task LoadData()
        //{
        //    var res = await Http.GetFromJsonAsync<List<EquipmentDto>>("api/Equipment/get-all");
        //    _equipments = res ?? new();
        //}
        private (string text, string color) GetMaintenanceText(DateTime? date)
        {
            if (date == null)
                return ("N/A", "black");

            var today = DateTime.Now.Date;
            var diff = (date.Value.Date - today).Days;

            if (diff > 0)
            {
                if (diff <= 5)
                    return ($"In {diff} days", "orange");

                return ($"In {diff} days", "black");
            }
            else if (diff < 0)
            {
                return ($"{Math.Abs(diff)} days overdue", "red");
            }
            else
            {
                return ("Today", "orange");
            }
        }
        private IEnumerable<EquipmentDto> GetFilteredData()
        {
            if (selectedTab == "Pending")
            {
                return _equipments.Where(x =>
                    x.BuyDate != null &&
                    x.BuyDate <= DateTime.Now);
            }

            return _equipments.Where(x =>
                x.BuyDate != null &&
                x.BuyDate > DateTime.Now);
        }
        private int GetPendingCount()
        {
            return _equipments.Count(x =>
                x.BuyDate != null &&
                x.BuyDate <= DateTime.Now);
        }
        private async Task LoadData()
        {
            await Task.Delay(300); // giả lập gọi API
            _equipments = new List<EquipmentDto>
            {
                new EquipmentDto
                {
                    EquipmentName = "CNC Lathe CNC-1",
                    EquipmentCode = "FNC-2023-001",
                    EquipmentModel = "FANUC α-D21MiB5",
                    EquipmentSerial = "SN123456",
                    Location = "Workshop A - Bay 01",
                    Status = "Running",
                    BuyDate = DateTime.Now.AddDays(-10),
                    MaintenanceCircleTime = 30
                },
                new EquipmentDto
                {
                    EquipmentName = "Vertical Machine VMC-1",
                    EquipmentCode = "MZK-2022-015",
                    EquipmentModel = "MAZAK VCN-530C",
                    EquipmentSerial = "SN789012",
                    Location = "Workshop A - Bay 02",
                    Status = "Running",
                    BuyDate = DateTime.Now.AddDays(-40),
                    MaintenanceCircleTime = 30
                },
                new EquipmentDto
                {
                    EquipmentName = "Hydraulic Press HP-500",
                    EquipmentCode = "YQ-2021-088",
                    EquipmentModel = "YQ32-500",
                    EquipmentSerial = "SN333333",
                    Location = "Workshop B - Bay 01",
                    Status = "Maintenance",
                    BuyDate = DateTime.Now.AddDays(-60),
                    MaintenanceCircleTime = 60
                }
            };
        }
    }
}
