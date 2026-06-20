using AntDesign;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Maintenance;
using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Components;
using CMMS.Client.Common;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace CMMS.Client.Modals.Maintenances
{
    public partial class MaintenanceModel
    {
        #region Declaration
        [Inject] private HttpClient Http { get; set; }
        private bool IsModalVisible = false;
        private MaintenanceDto MaintenanceDto { get; set; } = new();
        private List<VendorDto> Vendors = new();
        private List<UserDto> Users = new();
        private UserDto CurrentUser { get; set; } = new();
        private int eqId { get; set; }
        [Parameter] public EventCallback OnSave { get; set; }
        private Form<MaintenanceDto> formRef = new();

        private List<StatusItem> MaintenanceStatuses = new()
        {
            new() { Id = 1, Name = "Routine" },
            new() { Id = 2, Name = "Repair" }
        };

        public class StatusItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        #endregion
        #region Innit

        //private UserDto _selectedUserId;
        //private UserDto? User;

        //private UserDto SelectedUserId
        //{
        //    get => _selectedUserId;
        //    set
        //    {
        //        if (_selectedUserId != value)
        //        {
        //            _selectedUserId = value;
        //            MaintenanceDto.PICID = value?.WorkDayId;
        //            MaintenanceDto.MaintPIC = value?.FullName;
        //        }
        //    }
        //}

        public async Task ShowModal(int eqid)
        {
            var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
            CurrentUser = await CurrentUserClass.LoadCurrentUser();
            eqId = eqid;
            MaintenanceDto = new();
            await LoadVendorData();
            await LoadUsersData();
            IsModalVisible = true;
            await InvokeAsync(StateHasChanged);
        }
        #endregion
        #region Action
        private async Task Close()
        {
            IsModalVisible = false;
        }
        private async Task SaveAsync()
        {
            var selectedUser = Users.FirstOrDefault(u => u.WorkDayId == MaintenanceDto.PICID);
            MaintenanceDto.MaintPIC = selectedUser?.FullName;
            Console.WriteLine(selectedUser == null
                ? "USER NOT FOUND"
                : $"FOUND USER: {selectedUser.FullName}");
            var valid = formRef.Validate();
            if (!valid)
            {
                return;
            }
            else
            {
                await CreatedAsync(eqId);
            }    
            IsModalVisible = false;
        }
        private async Task CreatedAsync(int eqId)
        {
            try
            {
                var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
                CurrentUser = await CurrentUserClass.LoadCurrentUser();
                MaintenanceDto.WorkDayId = CurrentUser.WorkDayId;   
                var response = await Http.PostAsJsonAsync($"api/Maintenance/create/{eqId}",MaintenanceDto);
                if (response.IsSuccessStatusCode)
                {
                    await Message.Success("Tạo thành công!");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await Message.Error($"Tạo thất bại: {error}");
                }
                var update = await Http.PostAsync("api/equipment/update-status", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating maintenance: {ex.Message}");
            }
        }
        #endregion
        private async Task LoadVendorData()
        {
            Vendors = await Http.GetFromJsonAsync<List<VendorDto>>("api/vendor/vendors") ?? new();
        }
        private async Task LoadUsersData()
        {
            Users = await Http.GetFromJsonAsync<List<UserDto>>("api/user/users") ?? new();
        }
        
    }
}
