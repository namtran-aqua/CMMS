using AntDesign;
using CMMS.Client.Components.Equipments;
using CMMS.Client.Modals.Maintenances;
using CMMS.Shared.Dtos.DashBoards;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Maintenance;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.Maintenance
{
    public partial class Maintenance
    {
        [Inject] private HttpClient Http { get; set; }

        private List<EquipmentDto> _equipments = new();
        public List<DashBoarDto> DashBoardData { get; set; } = new();
        private List<DashBoarDto> DueSoon { get; set; } = new();
        private List<DashBoarDto> OverDue { get; set; } = new();
        private List<MaintenanceDto> _maintenances = new();
        private List<MaintenanceDto> MaintenanceGroups = new();
        private MaintenanceModel? _maintenanceModal;
        private Table<MaintenanceModel>? _tableRef;
        private bool isLoading = true;

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
        private async Task LoadData()
        {
            try
            {
                isLoading = true;

                var dashboardTask =
                    Http.GetFromJsonAsync<List<DashBoarDto>>(
                        "api/DashBoard/dashboard");

                var maintenanceTask =
                    Http.GetFromJsonAsync<List<MaintenanceDto>>(
                        "api/Maintenance/get-all");

                await Task.WhenAll(dashboardTask, maintenanceTask);

                DashBoardData = await dashboardTask ?? new();

                _maintenances = await maintenanceTask ?? new();
                MaintenanceGroups = _maintenances
    .GroupBy(x => x.EQID)
    .Select(g => new MaintenanceDto
    {
        EQID = g.Key,
        Items = g.ToList()
    })
    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }


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
            .ToList();
        }
        //private DateTime? GetNextMaintenanceDate(DashBoarDto item)
        //{
        //    if (item.NextMaintenanceDate.HasValue)
        //        return item.NextMaintenanceDate.Value;

        //    var baseDate = item.LastMaintenanceDate ?? item.BuyDate;
        //    if (baseDate.HasValue && item.MaintenanceCircleTime.HasValue)
        //    {
        //        return baseDate.Value.AddDays(item.MaintenanceCircleTime.Value);
        //    }

        //    return null;
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
        //private IEnumerable<EquipmentDto> GetFilteredData()
        //{
        //    if (selectedTab == "Pending")
        //    {
        //        return _equipments.Where(x =>
        //        {
        //            var nextDate = GetNextMaintenanceDate(x);
        //            return nextDate != null && nextDate <= DateTime.Now.AddDays(7);
        //        });
        //    }

        //    return _equipments.Where(x =>
        //    {
        //        var nextDate = GetNextMaintenanceDate(x);
        //        return nextDate != null && nextDate > DateTime.Now.AddDays(7);
        //    });
        //}
        //private int GetPendingCount()
        //{
        //    return _equipments.Count(x =>
        //    {
        //        var nextDate = GetNextMaintenanceDate(x);
        //        return nextDate != null && nextDate <= DateTime.Now.AddDays(7);
        //    });
        //}
        private async Task CreatedAsync(int eqid)
        {
            if (_maintenanceModal != null)
            {
                await _maintenanceModal.ShowModal(eqid);
            }
        }
        //private async Task HandleScrap(int eqId)
        //{
        //    var response = await Http.PostAsync(
        //        $"api/equipment/request/{eqId}",
        //        null);

        //    if (response.IsSuccessStatusCode)
        //    {
        //        await Message.Success("?ã g?i yêu c?u approve!");
        //    }
        //    else
        //    {
        //        var error = await response.Content.ReadAsStringAsync();

        //        await Message.Error(error);
        //    }
        //}
    }
}
