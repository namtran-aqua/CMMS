using CMMS.Client.Services;
using CMMS.Client.Modals.SpareParts;
using CMMS.Client.Components;
using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.Dtos.Common;
using CMMS.Shared.Dtos.Equipment;
using Microsoft.AspNetCore.Components;
using AntDesign;
using CMMS.Client.Common;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CMMS.Client.Pages.SpareParts.Tabs
{
    public partial class CatalogTab : ComponentBase, IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        [Inject] private IMessageService Message { get; set; }

        private List<SparePartDto> _allCatalogParts = new();
        private List<SparePartCategoryDto> Categories { get; set; } = new();
        private List<SparePartSupplierDto> Suppliers { get; set; } = new();
        private List<LocationDto> Locations { get; set; } = new();

        private int catalogPage = 1;
        private int catalogPageSize = 10;
        private string catalogSearchText = "";
        private string catalogPartCodeFilter = "";
        private string catalogPartNameFilter = "";
        private int catalogSupplierFilter = 0;
        private int catalogCategoryFilter = 0;
        private string catalogSortBy = "NameAsc";
        private bool isCatalogSearchPanelCollapsed = true;

        private List<SparePartDto> FilteredCatalogParts
        {
            get
            {
                var result = _allCatalogParts.AsEnumerable();

                // Filter by Factory
                if (FactoryState.SelectedFacId.HasValue)
                {
                    result = result.Where(x => x.FACID == FactoryState.SelectedFacId.Value);
                }

                // Filter by Search Text
                if (!string.IsNullOrWhiteSpace(catalogSearchText))
                {
                    var search = catalogSearchText.Trim().ToLower();
                    result = result.Where(x =>
                        (x.PartCode != null && x.PartCode.ToLower().Contains(search)) ||
                        (x.PartName != null && x.PartName.ToLower().Contains(search)) ||
                        (x.Note != null && x.Note.ToLower().Contains(search)) ||
                        (x.SupplierName != null && x.SupplierName.ToLower().Contains(search))
                    );
                }

                // Filter by Part Code
                if (!string.IsNullOrWhiteSpace(catalogPartCodeFilter))
                {
                    var code = catalogPartCodeFilter.Trim().ToLower();
                    result = result.Where(x => x.PartCode != null && x.PartCode.ToLower().Contains(code));
                }

                // Filter by Part Name
                if (!string.IsNullOrWhiteSpace(catalogPartNameFilter))
                {
                    var name = catalogPartNameFilter.Trim().ToLower();
                    result = result.Where(x => x.PartName != null && x.PartName.ToLower().Contains(name));
                }

                // Filter by Category
                if (catalogCategoryFilter > 0)
                {
                    result = result.Where(x => x.CategoryID == catalogCategoryFilter);
                }

                // Filter by Supplier
                if (catalogSupplierFilter>0)
                {
                    result = result.Where(x => x.SupplierID == catalogSupplierFilter);
                }

                // Sort
                result = catalogSortBy switch
                {
                    "NameAsc"  => result.OrderBy(x => x.PartName ?? ""),
                    "NameDesc" => result.OrderByDescending(x => x.PartName ?? ""),
                    "CodeAsc"  => result.OrderBy(x => x.PartCode ?? ""),
                    "PriceAsc" => result.OrderBy(x => x.Price),
                    "PriceDesc"=> result.OrderByDescending(x => x.Price),
                    _          => result.OrderBy(x => x.PartName ?? "")
                };

                return result.ToList();
            }
        }

        private string newCategoryName = "";
        private SparePartSupplierDto newSupplier = new();

        private SparePartDto? selectedPartForDetail;
        private bool isPartDetailModalVisible = false;

        private SparePartModal? _partModal;
        private ImportModal? _importModal;

        private bool IsAuthenticated { get; set; } = false;
        private UserDto CurrentUser { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
            CurrentUser = await CurrentUserClass.LoadCurrentUser();

            FactoryState.OnChange += OnFactoryChanged;

            await LoadLookupData();
            await LoadCatalogParts();
        }

        private async void OnFactoryChanged()
        {
            catalogPage = 1;
            await LoadCatalogParts();
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            FactoryState.OnChange -= OnFactoryChanged;
        }

        private async Task LoadLookupData()
        {
            try
            {
                var catTask = Http.GetFromJsonAsync<List<SparePartCategoryDto>>("api/SparePart/categories");
                var supTask = Http.GetFromJsonAsync<List<SparePartSupplierDto>>("api/SparePart/suppliers");
                var locTask = Http.GetFromJsonAsync<List<LocationDto>>("api/Location/locations");

                await Task.WhenAll(catTask, supTask, locTask);

                Categories = await catTask ?? new();
                Suppliers = await supTask ?? new();
                Locations = await locTask ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading lookup data: {ex.Message}");
            }
        }

        private async Task LoadCatalogParts()
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = "api/SparePart/get-all";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";

                _allCatalogParts = await Http.GetFromJsonAsync<List<SparePartDto>>(url) ?? new();
                catalogPage = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading catalog parts: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            catalogPage = 1;
            StateHasChanged();
        }

        private void OnCatalogPageChange(PaginationEventArgs args)
        {
            if (catalogPageSize != args.PageSize)
            {
                catalogPageSize = args.PageSize;
                catalogPage = 1;
            }
            else
            {
                catalogPage = args.Page;
            }
            StateHasChanged();
        }

        private void ResetFilters()
        {
            catalogSearchText = "";
            catalogPartCodeFilter = "";
            catalogPartNameFilter = "";
            catalogSupplierFilter = 0;
            catalogCategoryFilter = 0;
            catalogSortBy = "NameAsc";
            catalogPage = 1;
            StateHasChanged();
        }

        private void ShowPartDetail(SparePartDto part)
        {
            selectedPartForDetail = part;
            isPartDetailModalVisible = true;
        }

        private async Task ShowPartModal(SparePartDto? part)
        {
            if (_partModal != null)
                await _partModal.ShowModal(part);
        }

        private void ShowImportModal()
        {
            if (_importModal != null)
                _importModal.Show();
        }

        private async Task DeletePart(SparePartDto part)
        {
            var confirmed = await JS.InvokeAsync<bool>("confirm", $"Delete part '{part.PartName}'? This cannot be undone.");
            if (!confirmed) return;

            var response = await Http.DeleteAsync($"api/SparePart/delete/{part.SPID}");
            if (response.IsSuccessStatusCode)
            {
                Message.Success("Deleted successfully");
                await LoadCatalogParts();
            }
            else
            {
                Message.Error("Delete failed");
            }
        }

        private async Task AddCategory()
        {
            if (string.IsNullOrWhiteSpace(newCategoryName)) return;
            var response = await Http.PostAsJsonAsync("api/SparePart/category/create", new SparePartCategoryDto { CategoryName = newCategoryName });
            if (response.IsSuccessStatusCode)
            {
                newCategoryName = "";
                await LoadLookupData();
            }
        }

        private async Task DeleteCategory(int id)
        {
            var response = await Http.DeleteAsync($"api/SparePart/category/delete/{id}");
            if (response.IsSuccessStatusCode)
            {
                Message.Success("Deleted successfully");
                await LoadLookupData();
            }
            else
            {
                Message.Error("Delete failed");
            }
        }

        private async Task AddSupplier()
        {
            if (string.IsNullOrWhiteSpace(newSupplier.SupplierName)) return;
            var response = await Http.PostAsJsonAsync("api/SparePart/supplier/create", newSupplier);
            if (response.IsSuccessStatusCode)
            {
                newSupplier = new();
                await LoadLookupData();
            }
        }

        private async Task DeleteSupplier(int id)
        {
            var response = await Http.DeleteAsync($"api/SparePart/supplier/delete/{id}");
            if (response.IsSuccessStatusCode)
            {
                Message.Success("Deleted successfully");
                await LoadLookupData();
            }
            else
            {
                Message.Error("Delete failed");
            }
        }
    }
}
