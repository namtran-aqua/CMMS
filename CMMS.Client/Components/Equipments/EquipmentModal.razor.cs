using AntDesign;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace CMMS.Client.Components.Equipments
{
    public partial class EquipmentModal
    {
        #region Declaration
        [Inject] private HttpClient Http { get; set; }
        private bool IsModalVisible = false;
        private EquipmentDto EquipmentDto { get; set; } = new();
        private List<DepartmentDto> Departments  = new();
        private List<VendorDto> Vendors = new();
        private List<StatusUsingDto> StatusUsing = new();
        private List<UserDto> Users = new();    
        [Parameter] public EventCallback OnSave { get; set; }
        private Form<EquipmentDto> formRef = new();
        private bool IsEdit { get; set; }

        #endregion
        #region Innit
        public async Task ShowModal(bool isEdit, EquipmentDto? equipmentDto = null)
        {
            IsEdit = isEdit;
            EquipmentDto = new();
            await LoadDepartmentsData();
            await LoadStatusUsingData();
            await LoadVendorData();
            await LoadUsersData();
            if (equipmentDto != null)
            {
                EquipmentDto = equipmentDto;
            }
            IsModalVisible = true;
            await InvokeAsync(StateHasChanged);
        }
        #endregion
        #region Action
        private async Task SaveAsync()
        {
            var valid = formRef.Validate();
            if (!valid)
            {
                return;
            }
            if (IsEdit)
            {
                await UpdateAsync();
            }
            else
            {
                await CreatedAsync();
            }
            await OnSave.InvokeAsync();
            IsModalVisible = false;
        }
        private async Task Close()
        {
            IsModalVisible = false;
        }
        #endregion
        #region HandleData
        private async Task UpdateAsync()
        {
            var response = await Http.PutAsJsonAsync("api/Equipment/update", EquipmentDto);
            if (response.IsSuccessStatusCode)
            {
                await Message.Success("Cập nhật thành công !");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await Message.Error($"Cập nhật thất bại ! {error}");
            }
        }
        private async Task CreatedAsync()
        {
            var response = await Http.PostAsJsonAsync("api/Equipment/create", EquipmentDto);

            if (response.IsSuccessStatusCode)
            {

                await Message.Success("Tạo thành công!");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await Message.Error($"Tạo thất bại: {error}");
            }
        }
        #endregion
        private async Task LoadDepartmentsData()
        {
            Departments = await Http.GetFromJsonAsync<List<DepartmentDto>>("api/Department/departments") ?? new();
        }
        private async Task LoadStatusUsingData()
        {
            StatusUsing = await Http.GetFromJsonAsync<List<StatusUsingDto>>("api/StatusUsing/statususing") ?? new();
        }
        private async Task LoadVendorData()
        {
            Vendors = await Http.GetFromJsonAsync<List<VendorDto>>("api/Vendor/vendors") ?? new();
        }
        private async Task LoadUsersData()
        {
            Users = await Http.GetFromJsonAsync<List<UserDto>>("api/User/users") ?? new();
        }
    }

}
