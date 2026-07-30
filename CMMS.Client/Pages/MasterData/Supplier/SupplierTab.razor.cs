using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using CMMS.Shared.Dtos.SpareParts;
using AntDesign;

namespace CMMS.Client.Pages.MasterData.Supplier
{
    public partial class SupplierTab : ComponentBase
    {
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private IMessageService Message { get; set; } = default!;

        private List<SparePartSupplierDto> Suppliers { get; set; } = new();
        private bool isLoading = false;

        private bool isSupplierModalVisible = false;
        private string modalTitle = "Add Supplier";
        private SparePartSupplierDto editingSupplier = new();
        private bool isEditMode = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadSuppliers();
        }

        private async Task LoadSuppliers()
        {
            isLoading = true;
            try
            {
                var sups = await Http.GetFromJsonAsync<List<SparePartSupplierDto>>("api/SparePart/suppliers");
                if (sups != null)
                {
                    Suppliers = sups;
                }
            }
            catch (Exception ex)
            {
                Message.Error("Error loading suppliers: " + ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        private void ShowAddSupplierModal()
        {
            editingSupplier = new SparePartSupplierDto();
            isEditMode = false;
            modalTitle = "Add Supplier";
            isSupplierModalVisible = true;
        }

        private void ShowEditSupplierModal(SparePartSupplierDto supplier)
        {
            editingSupplier = new SparePartSupplierDto 
            { 
                SupplierID = supplier.SupplierID, 
                SupplierName = supplier.SupplierName,
                Phone = supplier.Phone,
                Email = supplier.Email
            };
            isEditMode = true;
            modalTitle = "Edit Supplier";
            isSupplierModalVisible = true;
        }

        private void HandleSupplierCancel()
        {
            isSupplierModalVisible = false;
        }

        private async Task HandleSupplierOk()
        {
            if (string.IsNullOrWhiteSpace(editingSupplier.SupplierName))
            {
                Message.Warning("Supplier name cannot be empty");
                return;
            }

            try
            {
                HttpResponseMessage response;
                if (isEditMode)
                {
                    response = await Http.PutAsJsonAsync("api/SparePart/supplier/update", editingSupplier);
                }
                else
                {
                    response = await Http.PostAsJsonAsync("api/SparePart/supplier/create", editingSupplier);
                }

                if (response.IsSuccessStatusCode)
                {
                    Message.Success(isEditMode ? "Supplier updated successfully" : "Supplier added successfully");
                    isSupplierModalVisible = false;
                    await LoadSuppliers();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Message.Error($"Failed to save supplier: {error}");
                }
            }
            catch (Exception ex)
            {
                Message.Error("Error saving supplier: " + ex.Message);
            }
        }

        private async Task DeleteSupplier(int id)
        {
            try
            {
                var response = await Http.DeleteAsync($"api/SparePart/supplier/delete/{id}");
                if (response.IsSuccessStatusCode)
                {
                    Message.Success("Supplier deleted successfully");
                    await LoadSuppliers();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Message.Error($"Failed to delete supplier: {error}");
                }
            }
            catch (Exception ex)
            {
                Message.Error("Error deleting supplier: " + ex.Message);
            }
        }
    }
}
