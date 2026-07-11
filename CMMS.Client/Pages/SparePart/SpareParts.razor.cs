using AntDesign;
using CMMS.Client.Common;
using CMMS.Client.Modals.SpareParts;
using CMMS.Client.Services;
using CMMS.Client.Components;
using CMMS.Shared.Authorization;
using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.Common;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.SpareParts
{
    public partial class SpareParts : IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }

        private List<SparePartDto> Parts { get; set; } = new();
        private List<SparePartCategoryDto> Categories { get; set; } = new();
        private List<SparePartSupplierDto> Suppliers { get; set; } = new();
        private List<SparePartTransactionDto> Transactions { get; set; } = new();
        private List<LocationDto> Locations { get; set; } = new();

        private SparePartModal? _partModal;
        private AdjustStockModal? _adjustModal;
        private MaintenanceExportModal? _exportModal;
        private ImportModal? _importModal;

        private string selectedTab = "Parts";

        private int partsPage = 1;
        private int partsPageSize = 10;
        private int historyPage = 1;
        private int historyPageSize = 10;

        private int totalPartsCount = 0;
        private int totalHistoryCount = 0;
        private int lowStockCount = 0;

        private bool IsAuthenticated { get; set; } = false;
        private UserDto CurrentUser { get; set; } = new();

        private string _searchText = "";
        private string searchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    partsPage = 1;
                    _ = LoadParts();
                }
            }
        }

        private int _categoryFilter = 0;
        private int categoryFilter
        {
            get => _categoryFilter;
            set
            {
                if (_categoryFilter != value)
                {
                    _categoryFilter = value;
                    partsPage = 1;
                    _ = LoadParts();
                }
            }
        }

        private string _stockFilter = "All";
        private string stockFilter
        {
            get => _stockFilter;
            set
            {
                if (_stockFilter != value)
                {
                    _stockFilter = value;
                    partsPage = 1;
                    _ = LoadParts();
                }
            }
        }

        private string _sortBy = "NameAsc";
        private string sortBy
        {
            get => _sortBy;
            set
            {
                if (_sortBy != value)
                {
                    _sortBy = value;
                    partsPage = 1;
                    _ = LoadParts();
                }
            }
        }

        private string _historySearchText = "";
        private string historySearchText
        {
            get => _historySearchText;
            set
            {
                if (_historySearchText != value)
                {
                    _historySearchText = value;
                    historyPage = 1;
                    _ = LoadHistory();
                }
            }
        }

        private string _historyTypeFilter = "";
        private string historyTypeFilter
        {
            get => _historyTypeFilter;
            set
            {
                if (_historyTypeFilter != value)
                {
                    _historyTypeFilter = value;
                    historyPage = 1;
                    _ = LoadHistory();
                }
            }
        }

        private string newCategoryName = "";
        private SparePartSupplierDto newSupplier = new();

        private int LowStockCount => lowStockCount;

        private List<SparePartDto> FilteredParts => Parts;

        private List<SparePartTransactionDto> FilteredHistory => Transactions;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
            CurrentUser = await CurrentUserClass.LoadCurrentUser();

            FactoryState.OnChange += OnFactoryChanged;

            await LoadLookupData();
            await LoadData();
        }

        private async void OnFactoryChanged()
        {
            partsPage = 1;
            historyPage = 1;
            await LoadData();
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

        private async Task LoadParts()
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = $"api/SparePart/get-paged?page={partsPage}&pageSize={partsPageSize}&searchText={Uri.EscapeDataString(searchText)}&categoryId={categoryFilter}&stockStatus={stockFilter}&sortBy={sortBy}";
                if (facId.HasValue)
                {
                    url += $"&factoryId={facId.Value}";
                }
                
                var res = await Http.GetFromJsonAsync<SparePartPagedResultDto>(url);
                if (res != null)
                {
                    Parts = res.Items ?? new();
                    totalPartsCount = res.TotalCount;
                    lowStockCount = res.LowStockCount;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading parts: {ex.Message}");
            }
        }

        private async Task LoadHistory()
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = $"api/SparePart/history-paged?page={historyPage}&pageSize={historyPageSize}&searchText={Uri.EscapeDataString(historySearchText)}&typeFilter={historyTypeFilter}";
                if (facId.HasValue)
                {
                    url += $"&factoryId={facId.Value}";
                }

                var res = await Http.GetFromJsonAsync<PagedResultDto<SparePartTransactionDto>>(url);
                if (res != null)
                {
                    Transactions = res.Items ?? new();
                    totalHistoryCount = res.TotalCount;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading transaction history: {ex.Message}");
            }
        }

        private async Task LoadData()
        {
            await LoadParts();
            await LoadHistory();
        }

        private async Task OnPartsPageChange(PaginationEventArgs args)
        {
            if (partsPageSize != args.PageSize)
            {
                partsPageSize = args.PageSize;
                partsPage = 1;
            }
            else
            {
                partsPage = args.Page;
            }
            await LoadParts();
            StateHasChanged();
        }

        private async Task OnHistoryPageChange(PaginationEventArgs args)
        {
            if (historyPageSize != args.PageSize)
            {
                historyPageSize = args.PageSize;
                historyPage = 1;
            }
            else
            {
                historyPage = args.Page;
            }
            await LoadHistory();
            StateHasChanged();
        }

        private string GetTxLabel(string type) => type switch
        {
            "IN" => "Stock in",
            "OUT" => "Stock out",
            "MAINTENANCE" => "Maintenance",
            _ => type
        };

        private string GetTxColor(string type) => type switch
        {
            "IN" => "#52c41a",
            "OUT" => "#fa8c16",
            "MAINTENANCE" => "#1677ff",
            _ => "#bfbfbf"
        };



        private async Task ShowPartModal(SparePartDto? part)
        {
            if (_partModal != null)
                await _partModal.ShowModal(part);
        }

        private async Task ShowAdjustStock(SparePartDto part)
        {
            if (_adjustModal != null)
                await _adjustModal.ShowModal(part);
        }

        private async Task ShowMaintenanceExport()
        {
            if (_exportModal != null)
                await _exportModal.ShowModal();
        }

        private async Task DeletePart(SparePartDto part)
        {
            var confirmed = await JS.InvokeAsync<bool>("confirm", $"Delete part '{part.PartName}'? This cannot be undone.");
            if (!confirmed) return;

            var response = await Http.DeleteAsync($"api/SparePart/delete/{part.SPID}");
            if (response.IsSuccessStatusCode)
            {
                Message.Success("Deleted successfully");
                await LoadData();
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
                await LoadData();
            }
        }

        private async Task DeleteCategory(int id)
        {
            await Http.DeleteAsync($"api/SparePart/category/delete/{id}");
            await LoadData();
        }

        private async Task AddSupplier()
        {
            if (string.IsNullOrWhiteSpace(newSupplier.SupplierName)) return;
            var response = await Http.PostAsJsonAsync("api/SparePart/supplier/create", newSupplier);
            if (response.IsSuccessStatusCode)
            {
                newSupplier = new();
                await LoadData();
            }
        }

        private void ShowImportModal()
        {
            if (_importModal != null)
                _importModal.Show();
        }

        private async Task DeleteSupplier(int id)
        {
            await Http.DeleteAsync($"api/SparePart/supplier/delete/{id}");
            await LoadData();
        }
    }
}