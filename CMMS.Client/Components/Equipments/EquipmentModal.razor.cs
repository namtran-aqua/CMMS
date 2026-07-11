using AntDesign;
using CMMS.Client.Common;
using CMMS.Client.Services;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;


namespace CMMS.Client.Components.Equipments
{
    public partial class EquipmentModal
    {
        #region Declaration
        [Inject] private HttpClient Http { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        private bool IsAuthenticated { get; set; } = false;


        private bool IsModalVisible = false;
        private EquipmentDto EquipmentDto { get; set; } = new();
        private List<DepartmentDto> Departments  = new();
        private List<LocationDto> Locations = new();
        private List<VendorDto> Vendors = new();
        //private List<StatusUsingDto> StatusUsing = new();
        private List<UserDto> Users = new();
        private UserDto CurrentUser { get; set; } = new();
        private record CurrencyOption(string Id, string Name);

        /// <summary>
        /// Filter departments theo FACID của CurrentUser.
        /// Nếu user chưa có FACID thì hiện tất cả.
        /// </summary>
        private List<DepartmentDto> FilteredDepartments =>
            CurrentUser.FACID.HasValue
                ? Departments.Where(d => d.FACID == CurrentUser.FACID).ToList()
                : Departments;

        

        private List<CurrencyOption> CurrencyList = new()
        {
            new("VND", "VND"),
            new("USD", "USD"),
            new("CNY", "CNY (NDT)"),
        };
        [Parameter] public EventCallback OnSave { get; set; }
        private Form<EquipmentDto> formRef = new();
        private bool IsEdit { get; set; }

        #endregion
        #region Innit
        public async Task ShowModal(bool isEdit, EquipmentDto? equipmentDto = null)
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
            CurrentUser = await CurrentUserClass.LoadCurrentUser();
            IsEdit = isEdit;
            EquipmentDto = new();
            await Task.WhenAll(
                LoadDepartmentsData(),
                LoadLocationsData(),
                LoadVendorData(),
                LoadUsersData()
            );
            if (equipmentDto != null)
            {
                EquipmentDto = equipmentDto;

                Console.WriteLine($"PICID = [{EquipmentDto.PICID}]");
                Console.WriteLine($"PIC   = [{EquipmentDto.PIC}]");
            }
            IsModalVisible = true;
            await InvokeAsync(StateHasChanged);
        }
        #endregion
        #region Action
        private async Task SaveAsync()
        {
            var selectedUser = Users.FirstOrDefault(u => u.WorkDayId == EquipmentDto.PICID);
            EquipmentDto.PIC = selectedUser?.FullName;
            Console.WriteLine(selectedUser == null
                ? "USER NOT FOUND"
                : $"FOUND USER: {selectedUser.FullName}");
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
            var response = await Http.PutAsJsonAsync("api/equipment/update", EquipmentDto);
            if (response.IsSuccessStatusCode)
            {
                Message.Success("Cập nhật thành công !");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Message.Error($"Cập nhật thất bại ! {error}");
            }
        }
        private async Task CreatedAsync()
        {
            var response = await Http.PostAsJsonAsync("api/equipment/create", EquipmentDto);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Message.Error($"Tạo thất bại: {error}");
                return;
            }

            Message.Success("Tạo thành công!");

            var response2 = await Http.PostAsync("api/equipment/update-status", null);

            if (!response2.IsSuccessStatusCode)
            {
                var error = await response2.Content.ReadAsStringAsync();
                Message.Warning($"Đã tạo thiết bị nhưng cập nhật trạng thái thất bại. {error}");
            }
        }
        #endregion
        private async Task LoadDepartmentsData()
        {
            if (Departments != null && Departments.Any()) return;
            Departments = await Http.GetFromJsonAsync<List<DepartmentDto>>("api/department/departments") ?? new();
        }
        private async Task LoadLocationsData()
        {
            if (Locations != null && Locations.Any()) return;
            Locations = await Http.GetFromJsonAsync<List<LocationDto>>("api/location/locations") ?? new();
        }
        //private async Task LoadStatusUsingData()
        //{
        //    StatusUsing = await Http.GetFromJsonAsync<List<StatusUsingDto>>("api/statusUsing/statususing") ?? new();
        //}
        private async Task LoadVendorData()
        {
            if (Vendors != null && Vendors.Any()) return;
            Vendors = await Http.GetFromJsonAsync<List<VendorDto>>("api/vendor/vendors") ?? new();
        }
        private async Task LoadUsersData()
        {
            if (Users != null && Users.Any()) return;
            Users = await Http.GetFromJsonAsync<List<UserDto>>("api/user/users") ?? new();
        }

    }

}
