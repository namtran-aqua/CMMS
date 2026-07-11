using AntDesign;
using CMMS.Client.Common;
using CMMS.Client.Modals.SpareParts;
using CMMS.Client.Services;
using CMMS.Shared.Authorization;
using CMMS.Shared.Dtos.SpareParts;
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

        private string selectedTab = "Parts";

        private int partsPage = 1;
        private int partsPageSize = 10;
        private int historyPage = 1;
        private int historyPageSize = 10;

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
                }
            }
        }

        private string newCategoryName = "";
        private SparePartSupplierDto newSupplier = new();

        private int LowStockCount => Parts?.Count(p => p.IsLowStock) ?? 0;

        private List<SparePartDto> FilteredParts
        {
            get
            {
                if (Parts == null) return new();
                var result = Parts.AsEnumerable();

                // Filter by Factory
                if (FactoryState.SelectedFacId.HasValue)
                {
                    result = result.Where(p => p.FACID == FactoryState.SelectedFacId.Value);
                }

                if (categoryFilter > 0)
                    result = result.Where(p => p.CategoryID == categoryFilter);

                // Filter by Stock Status
                result = stockFilter switch
                {
                    "Low" => result.Where(p => p.IsLowStock),
                    "InStock" => result.Where(p => p.Inventory > p.MinStock),
                    "Out" => result.Where(p => p.Inventory <= 0),
                    _ => result
                };

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var s = searchText.Trim().ToLower();
                    result = result.Where(p =>
                        (p.PartCode != null && p.PartCode.ToLower().Contains(s)) ||
                        (p.PartName != null && p.PartName.ToLower().Contains(s)) ||
                        (p.Location != null && p.Location.ToLower().Contains(s)) ||
                        (p.SupplierName != null && p.SupplierName.ToLower().Contains(s)));
                }

                // Sorting
                result = sortBy switch
                {
                    "NameAsc" => result.OrderBy(p => p.PartName ?? ""),
                    "NameDesc" => result.OrderByDescending(p => p.PartName ?? ""),
                    "CodeAsc" => result.OrderBy(p => p.PartCode ?? ""),
                    "PriceAsc" => result.OrderBy(p => p.Price ?? 0),
                    "PriceDesc" => result.OrderByDescending(p => p.Price ?? 0),
                    "StockAsc" => result.OrderBy(p => p.Inventory ?? 0),
                    "StockDesc" => result.OrderByDescending(p => p.Inventory ?? 0),
                    _ => result.OrderBy(p => p.PartName ?? "")
                };

                return result.ToList();
            }
        }

        private List<SparePartTransactionDto> FilteredHistory
        {
            get
            {
                if (Transactions == null) return new();
                var result = Transactions.AsEnumerable();

                // Filter by Factory
                if (FactoryState.SelectedFacId.HasValue)
                {
                    result = result.Where(t => t.FACID == FactoryState.SelectedFacId.Value);
                }

                if (!string.IsNullOrWhiteSpace(historyTypeFilter))
                    result = result.Where(t => t.Type == historyTypeFilter);

                if (!string.IsNullOrWhiteSpace(historySearchText))
                {
                    var s = historySearchText.Trim().ToLower();
                    result = result.Where(t =>
                        (t.PartCode != null && t.PartCode.ToLower().Contains(s)) ||
                        (t.PartName != null && t.PartName.ToLower().Contains(s)) ||
                        (t.Equipment != null && t.Equipment.ToLower().Contains(s)));
                }

                return result.OrderByDescending(t => t.Date).ToList();
            }
        }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
            CurrentUser = await CurrentUserClass.LoadCurrentUser();

            // Subscribe factory change event
            FactoryState.OnChange += OnFactoryChanged;

            await LoadData();
        }

        private async void OnFactoryChanged()
        {
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            FactoryState.OnChange -= OnFactoryChanged;
        }

        private async Task LoadData()
        {
            try
            {
                var partsTask = Http.GetFromJsonAsync<List<SparePartDto>>("api/SparePart/get-all");
                var catTask = Http.GetFromJsonAsync<List<SparePartCategoryDto>>("api/SparePart/categories");
                var supTask = Http.GetFromJsonAsync<List<SparePartSupplierDto>>("api/SparePart/suppliers");
                var historyTask = Http.GetFromJsonAsync<List<SparePartTransactionDto>>("api/SparePart/history");
                var locTask = Http.GetFromJsonAsync<List<LocationDto>>("api/Location/locations");

                await Task.WhenAll(partsTask, catTask, supTask, historyTask, locTask);

                Parts = await partsTask ?? new();
                Categories = await catTask ?? new();
                Suppliers = await supTask ?? new();
                Transactions = await historyTask ?? new();
                Locations = await locTask ?? new();

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading spare parts: {ex.Message}");
            }
        }

        private void OnPartsPageChange(PaginationEventArgs args)
        {
            if (partsPageSize != args.PageSize)
            {
                partsPageSize = args.PageSize;
                partsPage = 1; // đổi page size thì về lại trang đầu
            }
            else
            {
                partsPage = args.Page;
            }
            StateHasChanged();
        }

        private void OnHistoryPageChange(PaginationEventArgs args)
        {
            if (historyPageSize != args.PageSize)
            {
                historyPageSize = args.PageSize;
                historyPage = 1; // đổi page size thì về lại trang đầu
            }
            else
            {
                historyPage = args.Page;
            }
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

        private async Task DeleteSupplier(int id)
        {
            await Http.DeleteAsync($"api/SparePart/supplier/delete/{id}");
            await LoadData();
        }
    }
}