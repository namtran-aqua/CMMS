using AntDesign;
using CMMS.Client.Components.Equipments;
using CMMS.Shared.EquipmentDto;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;
namespace CMMS.Client.Pages.Equipment
{
    public partial class Equipment
    {
        #region Declaration
        [Inject] private HttpClient Http { get; set; }

        private List<EquipmentDto> _equipments = new();
        private EquipmentModal? equipmentModal;
        #endregion

        #region Innit
        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                var res = await Http.GetFromJsonAsync<List<EquipmentDto>>("api/Equipment/get-all");
                _equipments = res ?? new List<EquipmentDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading users: {ex.Message}");
            }

        }
        #endregion
        #region Action
        private string GetStatusClass(string? status)
        {
            return status switch
            {
                "Running" => "badge bg-dark",
                "Fault" => "badge bg-danger",
                "Maintenance" => "badge bg-secondary",
                "Stopped" => "badge bg-light text-dark",
                _ => "badge bg-secondary"
            };
        }
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
        private async Task CreatedAsync()
        {
            if(equipmentModal != null)
            {
                await equipmentModal.ShowModal(false);
            }
        }
        private async Task EditAsync(EquipmentDto equipmentDto)
        {
            if (equipmentModal != null)
            {
                await equipmentModal.ShowModal(true, equipmentDto);
            }
        }
        private async Task DeleteAsync(int id)
        {
            var confirm = await JS.InvokeAsync<bool>("confirm", "Bạn có chắc chắn muốn xóa không?");
            if (!confirm)
                return;
            var response = await Http.DeleteAsync($"api/Equipment/delete/{id}");
            if (response.IsSuccessStatusCode)
            {
                await Message.Success("Xóa thành công !");
            }
            else
            {
                await Message.Error("Xóa thất bại !");
            }
            await LoadData();
            await InvokeAsync(StateHasChanged);
        }
        #endregion
    }
}
