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
    public partial class InventoryTab : ComponentBase, IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        [Inject] private IMessageService Message { get; set; }

        private List<SparePartDto> _allParts = new();
        private List<SparePartCategoryDto> Categories { get; set; } = new();
        private List<SparePartSupplierDto> Suppliers { get; set; } = new();
        private List<LocationDto> Locations { get; set; } = new();

        private int partsPage = 1;
        private int partsPageSize = 10;
        private string searchText = "";
        private string partCodeFilter = "";
        private string partNameFilter = "";
        private int supplierFilter = 0;
        private int categoryFilter = 0;
        private string stockFilter = "All";
        private string sortBy = "NameAsc";
        private bool isSearchPanelCollapsed = true;

        private List<SparePartDto> FilteredParts
        {
            get
            {
                var result = _allParts.AsEnumerable();

                // Filter by Factory
                if (FactoryState.SelectedFacId.HasValue)
                {
                    result = result.Where(x => x.FACID == FactoryState.SelectedFacId.Value);
                }

                // Filter by Search Text
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var search = searchText.Trim().ToLower();
                    result = result.Where(x =>
                        (x.PartCode != null && x.PartCode.ToLower().Contains(search)) ||
                        (x.PartName != null && x.PartName.ToLower().Contains(search)) ||
                        (x.Note != null && x.Note.ToLower().Contains(search)) ||
                        (x.SupplierName != null && x.SupplierName.ToLower().Contains(search))
                    );
                }

                // Filter by Part Code
                if (!string.IsNullOrWhiteSpace(partCodeFilter))
                {
                    var code = partCodeFilter.Trim().ToLower();
                    result = result.Where(x => x.PartCode != null && x.PartCode.ToLower().Contains(code));
                }

                // Filter by Part Name
                if (!string.IsNullOrWhiteSpace(partNameFilter))
                {
                    var name = partNameFilter.Trim().ToLower();
                    result = result.Where(x => x.PartName != null && x.PartName.ToLower().Contains(name));
                }

                // Filter by Category
                if (categoryFilter > 0)
                {
                    result = result.Where(x => x.CategoryID == categoryFilter);
                }

                // Filter by Supplier
                if (supplierFilter > 0)
                {
                    result = result.Where(x => x.SupplierID == supplierFilter);
                }

                // Filter by Stock Status
                result = stockFilter switch
                {
                    "Low" => result.Where(x => (x.Inventory ?? 0) <= (x.MinStock ?? 0)),
                    "InStock" => result.Where(x => (x.Inventory ?? 0) > 0),
                    "Out" => result.Where(x => (x.Inventory ?? 0) <= 0),
                    _ => result
                };

                // Sort
                result = sortBy switch
                {
                    "NameAsc"  => result.OrderBy(x => x.PartName ?? ""),
                    "NameDesc" => result.OrderByDescending(x => x.PartName ?? ""),
                    "CodeAsc"  => result.OrderBy(x => x.PartCode ?? ""),
                    "StockAsc" => result.OrderBy(x => x.Inventory ?? 0),
                    "StockDesc"=> result.OrderByDescending(x => x.Inventory ?? 0),
                    _          => result.OrderBy(x => x.PartName ?? "")
                };

                return result.ToList();
            }
        }

        private SparePartDto? selectedPartForDetail;
        private bool isPartDetailModalVisible = false;

        private SparePartDto? selectedPartForHistory;
        private List<SparePartTransactionDto> partMovementHistory = new();
        private bool isPartHistoryModalVisible = false;

        private SparePartModal? _partModal;
        private AdjustStockModal? _adjustModal;
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
            await LoadParts();
        }

        private async void OnFactoryChanged()
        {
            partsPage = 1;
            await LoadParts();
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
                var url = "api/SparePart/get-all";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";

                _allParts = await Http.GetFromJsonAsync<List<SparePartDto>>(url) ?? new();
                partsPage = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading parts: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            partsPage = 1;
            StateHasChanged();
        }

        private void OnPartsPageChange(PaginationEventArgs args)
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
            StateHasChanged();
        }

        private void ResetFilters()
        {
            searchText = "";
            partCodeFilter = "";
            partNameFilter = "";
            supplierFilter = 0;
            categoryFilter = 0;
            stockFilter = "All";
            sortBy = "NameAsc";
            partsPage = 1;
            StateHasChanged();
        }

        private void ShowPartDetail(SparePartDto part)
        {
            selectedPartForDetail = part;
            isPartDetailModalVisible = true;
        }

        private async Task ShowPartHistory(SparePartDto part)
        {
            selectedPartForHistory = part;
            partMovementHistory.Clear();
            isPartHistoryModalVisible = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                var res = await Http.GetFromJsonAsync<List<SparePartTransactionDto>>($"api/SparePart/item-history/{part.SPID}");
                if (res != null)
                {
                    partMovementHistory = res;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading part history: {ex.Message}");
            }
            await InvokeAsync(StateHasChanged);
        }

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
                await LoadParts();
            }
            else
            {
                Message.Error("Delete failed");
            }
        }

        private async Task TriggerExcelExport()
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = "api/SparePart/export-excel";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";

                var response = await Http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var fileName = $"SparePart_Inventory_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    var base64 = Convert.ToBase64String(fileBytes);
                    await JS.InvokeVoidAsync("CMMSJsFunctions.saveAsFile", fileName, base64);
                    Message.Success("Exported successfully!");
                }
                else
                {
                    Message.Error("Export failed.");
                }
            }
            catch (Exception ex)
            {
                Message.Error($"Export Error: {ex.Message}");
            }
        }

        private string GetTxLabel(string type) => type switch
        {
            "IN" => "In",
            "OUT" => "Out",
            "REVERSAL" => "Reversal",
            _ => type
        };

        private string GetTxColor(string type) => type switch
        {
            "IN" => "#52c41a",
            "OUT" => "#fa8c16",
            "REVERSAL" => "#ff4d4f",
            _ => "#bfbfbf"
        };
    }
}
