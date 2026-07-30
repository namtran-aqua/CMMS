using AntDesign;
using CMMS.Shared.Dtos.Equipment;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.MasterData.Location
{
    public partial class LocationTab
    {
        [Inject]
        public HttpClient Http { get; set; }

        [Inject]
        public IMessageService Message { get; set; }

        public List<LocationDto> Locations { get; set; } = new();
        public LocationDto editingLocation { get; set; } = new();
        public bool isLocationModalVisible = false;
        public string modalTitle = "Add Location";
        public bool isEditMode = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadLocations();
        }

        public async Task LoadLocations()
        {
            try
            {
                Locations = await Http.GetFromJsonAsync<List<LocationDto>>("api/Location/get-all") ?? new List<LocationDto>();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Message.Error("Failed to load locations: " + ex.Message);
            }
        }

        public void ShowAddLocationModal()
        {
            editingLocation = new LocationDto();
            modalTitle = "Add Location";
            isEditMode = false;
            isLocationModalVisible = true;
        }

        public void ShowEditLocationModal(LocationDto location)
        {
            editingLocation = new LocationDto
            {
                LocID = location.LocID,
                DeptID = location.DeptID,
                FACID = location.FACID,
                LocName = location.LocName,
                LocCode = location.LocCode,
                LocManager = location.LocManager
            };
            modalTitle = "Edit Location";
            isEditMode = true;
            isLocationModalVisible = true;
        }

        public async Task HandleLocationOk()
        {
            if (string.IsNullOrWhiteSpace(editingLocation.LocName))
            {
                Message.Warning("Location Name is required.");
                return;
            }

            HttpResponseMessage response;
            if (isEditMode)
            {
                response = await Http.PutAsJsonAsync("api/Location", editingLocation);
            }
            else
            {
                response = await Http.PostAsJsonAsync("api/Location", editingLocation);
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CMMS.Shared.Dtos.Common.ApiResponse>();
                if (result != null && result.Success)
                {
                    Message.Success(result.Message);
                    isLocationModalVisible = false;
                    await LoadLocations();
                }
                else
                {
                    Message.Error(result?.Message ?? "Action failed.");
                }
            }
            else
            {
                Message.Error("Network error occurred.");
            }
        }

        public void HandleLocationCancel()
        {
            isLocationModalVisible = false;
        }

        public async Task DeleteLocation(int? id)
        {
            if (id == null) return;
            var response = await Http.DeleteAsync($"api/Location/{id}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CMMS.Shared.Dtos.Common.ApiResponse>();
                if (result != null && result.Success)
                {
                    Message.Success(result.Message);
                    await LoadLocations();
                }
                else
                {
                    Message.Error(result?.Message ?? "Delete failed.");
                }
            }
            else
            {
                Message.Error("Network error occurred.");
            }
        }
    }
}
