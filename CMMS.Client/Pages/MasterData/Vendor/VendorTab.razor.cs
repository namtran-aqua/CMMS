using AntDesign;
using CMMS.Shared.Dtos.Equipment;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.MasterData.Vendor
{
    public partial class VendorTab
    {
        [Inject]
        public HttpClient Http { get; set; }

        [Inject]
        public IMessageService Message { get; set; }

        public List<VendorDto> Vendors { get; set; } = new();
        public VendorDto editingVendor { get; set; } = new();
        public bool isVendorModalVisible = false;
        public string modalTitle = "Add Vendor";
        public bool isEditMode = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadVendors();
        }

        public async Task LoadVendors()
        {
            try
            {
                Vendors = await Http.GetFromJsonAsync<List<VendorDto>>("api/Vendor/get-all") ?? new List<VendorDto>();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Message.Error("Failed to load vendors: " + ex.Message);
            }
        }

        public void ShowAddVendorModal()
        {
            editingVendor = new VendorDto();
            modalTitle = "Add Vendor";
            isEditMode = false;
            isVendorModalVisible = true;
        }

        public void ShowEditVendorModal(VendorDto vendor)
        {
            editingVendor = new VendorDto
            {
                VendorID = vendor.VendorID,
                VendorName = vendor.VendorName,
                VendorCode = vendor.VendorCode,
                VendorAddress = vendor.VendorAddress,
                VendorEmail = vendor.VendorEmail,
                VendorPhone = vendor.VendorPhone,
                VendorContact = vendor.VendorContact,
                VendorNote = vendor.VendorNote,
                VendorBankName = vendor.VendorBankName
            };
            modalTitle = "Edit Vendor";
            isEditMode = true;
            isVendorModalVisible = true;
        }

        public async Task HandleVendorOk()
        {
            if (string.IsNullOrWhiteSpace(editingVendor.VendorName))
            {
                Message.Warning("Vendor Name is required.");
                return;
            }

            HttpResponseMessage response;
            if (isEditMode)
            {
                response = await Http.PutAsJsonAsync("api/Vendor", editingVendor);
            }
            else
            {
                response = await Http.PostAsJsonAsync("api/Vendor", editingVendor);
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CMMS.Shared.Dtos.Common.ApiResponse>();
                if (result != null && result.Success)
                {
                    Message.Success(result.Message);
                    isVendorModalVisible = false;
                    await LoadVendors();
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

        public void HandleVendorCancel()
        {
            isVendorModalVisible = false;
        }

        public async Task DeleteVendor(int? id)
        {
            if (id == null) return;
            var response = await Http.DeleteAsync($"api/Vendor/{id}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CMMS.Shared.Dtos.Common.ApiResponse>();
                if (result != null && result.Success)
                {
                    Message.Success(result.Message);
                    await LoadVendors();
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
