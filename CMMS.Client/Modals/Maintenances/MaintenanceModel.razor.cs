using AntDesign;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Maintenance;
using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Components;
using SixLabors.ImageSharp.Metadata;
using System.Net.Http.Json;

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
        private int eqId { get; set; }
        [Parameter] public EventCallback OnSave { get; set; }
        private Form<MaintenanceDto> formRef = new();
        #endregion
        #region Innit



        private UserDto _selectedUserId;

        private UserDto SelectedUserId
        {
            get => _selectedUserId;
            set
            {
                if (_selectedUserId != value)
                {
                    _selectedUserId = value;

                
                    MaintenanceDto.PICID = value?.WorkDayId;
                    MaintenanceDto.MaintPIC = value?.FullName;
                }
            }
        }

        private UserDto? User;

        public async Task ShowModal(int eqid)
        {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating maintenance: {ex.Message}");
            }
        }
        #endregion
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
