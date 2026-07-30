using AntDesign;
using CMMS.Shared.Dtos.Equipment;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.MasterData.Factory
{
    public partial class FactoryTab
    {
        [Inject]
        public HttpClient Http { get; set; }

        [Inject]
        public IMessageService Message { get; set; }

        public List<FactoryDto> Factories { get; set; } = new();
        public FactoryDto editingFactory { get; set; } = new();
        public bool isFactoryModalVisible = false;
        public string modalTitle = "Add Factory";
        public bool isEditMode = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadFactories();
        }

        public async Task LoadFactories()
        {
            try
            {
                Factories = await Http.GetFromJsonAsync<List<FactoryDto>>("api/Factory/get-all") ?? new List<FactoryDto>();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Message.Error("Failed to load factories: " + ex.Message);
            }
        }

        public void ShowAddFactoryModal()
        {
            editingFactory = new FactoryDto();
            modalTitle = "Add Factory";
            isEditMode = false;
            isFactoryModalVisible = true;
        }

        public void ShowEditFactoryModal(FactoryDto factory)
        {
            editingFactory = new FactoryDto
            {
                FACID = factory.FACID,
                FACCode = factory.FACCode,
                FACName = factory.FACName,
                FACFullName = factory.FACFullName
            };
            modalTitle = "Edit Factory";
            isEditMode = true;
            isFactoryModalVisible = true;
        }

        public async Task HandleFactoryOk()
        {
            if (string.IsNullOrWhiteSpace(editingFactory.FACName))
            {
                Message.Warning("Factory Name is required.");
                return;
            }

            HttpResponseMessage response;
            if (isEditMode)
            {
                response = await Http.PutAsJsonAsync("api/Factory", editingFactory);
            }
            else
            {
                response = await Http.PostAsJsonAsync("api/Factory", editingFactory);
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CMMS.Shared.Dtos.Common.ApiResponse>();
                if (result != null && result.Success)
                {
                    Message.Success(result.Message);
                    isFactoryModalVisible = false;
                    await LoadFactories();
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

        public void HandleFactoryCancel()
        {
            isFactoryModalVisible = false;
        }

        public async Task DeleteFactory(int? id)
        {
            if (id == null) return;
            var response = await Http.DeleteAsync($"api/Factory/{id}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CMMS.Shared.Dtos.Common.ApiResponse>();
                if (result != null && result.Success)
                {
                    Message.Success(result.Message);
                    await LoadFactories();
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
