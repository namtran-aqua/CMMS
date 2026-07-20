using AntDesign;
using CMMS.Client.Common;
using CMMS.Client.Services;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System.Net.Http.Json;

namespace CMMS.Client.Modals.SpareParts
{
    public partial class SparePartModal
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        [Parameter] public EventCallback OnSave { get; set; }
        [Parameter] public List<SparePartCategoryDto> Categories { get; set; } = new();
        [Parameter] public List<SparePartSupplierDto> Suppliers { get; set; } = new();
        [Parameter] public List<LocationDto> Locations { get; set; } = new();
        private bool IsAuthenticated { get; set; } = false;

        private bool IsModalVisible = false;
        private SparePartDto PartDto { get; set; } = new();

        private UserDto CurrentUser { get; set; } = new();
        private List<DepartmentDto> Departments = new();
        private Form<SparePartDto> formRef = new();
        private List<DepartmentDto> FilteredDepartments =>
            CurrentUser.FACID.HasValue
                ? Departments.Where(d => d.FACID == CurrentUser.FACID).ToList()
                : Departments;
        
        public async Task ShowModal(SparePartDto? part)
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
            CurrentUser = await CurrentUserClass.LoadCurrentUser();
            await LoadDepartmentsData();

            PartDto = part == null ? new SparePartDto {IsCoded = false } : new SparePartDto
            {
                SPID = part.SPID,
                PartCode = part.PartCode,
                PartName = part.PartName,
                CategoryID = part.CategoryID,
                Unit = part.Unit,
                Price = part.Price,
                Inventory = part.Inventory,
                MinStock = part.MinStock,
                LocID = part.LocID,
                SupplierID = part.SupplierID,
                Note = part.Note,
                IsCoded = part.IsCoded,
                ImageUrl = part.ImageUrl,
                FACID = part.FACID
            };

            IsModalVisible = true;
            await InvokeAsync(StateHasChanged);
        }

        private Task Close()
        {
            IsModalVisible = false;
            return Task.CompletedTask;
        }

        private void OnImageUploadCompleted(UploadInfo fileinfo)
        {
            if (fileinfo.File.State == UploadState.Success)
            {
                var url = fileinfo.File.Response?.ToString();
                if (!string.IsNullOrEmpty(url))
                {
                    PartDto.ImageUrl = url;
                    StateHasChanged();
                }
            }
        }

        private async Task SaveAsync()
        {
            var valid = formRef.Validate();
            if (!valid) return;

            try
            {
                if (PartDto.SPID == 0 && !PartDto.FACID.HasValue)
                {
                    PartDto.FACID = CurrentUser.FACID ?? FactoryState.SelectedFacId;
                }

                HttpResponseMessage response;
                if (PartDto.SPID == 0)
                    response = await Http.PostAsJsonAsync("api/SparePart/create", PartDto);
                else
                    response = await Http.PutAsJsonAsync("api/SparePart/update", PartDto);

                if (response.IsSuccessStatusCode)
                {
                    Message.Success("Saved successfully");
                    IsModalVisible = false;
                    await OnSave.InvokeAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Message.Error($"Save failed: {error}");
                }
            }
            catch (Exception ex)
            {
                Message.Error($"Error: {ex.Message}");
            }
        }

        private async Task LoadDepartmentsData()
        {
            if (Departments != null && Departments.Any()) return;
            Departments = await Http.GetFromJsonAsync<List<DepartmentDto>>("api/department/departments") ?? new();
        }
    }
}