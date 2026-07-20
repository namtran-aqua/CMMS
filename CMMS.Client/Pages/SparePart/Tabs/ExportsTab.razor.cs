using CMMS.Client.Services;
using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.Dtos.Common;
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
    public partial class ExportsTab : ComponentBase, IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        [Inject] private IMessageService Message { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }

        private List<ExportOrderDto> _allExportOrders = new();
        private List<MovementTypeDto> MovementTypes { get; set; } = new();
        private List<SparePartDto> exportPartsSearchList { get; set; } = new();

        private int exportPage = 1;
        private int exportPageSize = 10;
        private string exportCodeSearch = "";
        private int? exportMovementTypeFilter = 0;
        private DateTime? exportFromDate = null;
        private DateTime? exportToDate = null;
        private bool isCatalogSearchPanelCollapsed = true;

        private List<ExportOrderDto> FilteredExportOrders
        {
            get
            {
                var result = _allExportOrders.AsEnumerable();

                // Filter by Factory
                if (FactoryState.SelectedFacId.HasValue)
                {
                    result = result.Where(x => x.FACID == FactoryState.SelectedFacId.Value);
                }

                // Filter by export code search
                if (!string.IsNullOrWhiteSpace(exportCodeSearch))
                {
                    var search = exportCodeSearch.Trim().ToLower();
                    result = result.Where(x => x.ExportCode != null && x.ExportCode.ToLower().Contains(search));
                }

                // Filter by movement type
                if (exportMovementTypeFilter > 0)
                {
                    result = result.Where(x => x.MovementTypeID == exportMovementTypeFilter.Value);
                }

                // Filter by from date
                if (exportFromDate.HasValue)
                {
                    result = result.Where(x => x.ExportDate.Date >= exportFromDate.Value.Date);
                }

                // Filter by to date
                if (exportToDate.HasValue)
                {
                    result = result.Where(x => x.ExportDate.Date <= exportToDate.Value.Date);
                }

                return result.ToList();
            }
        }

        private ExportOrderDto? selectedExportOrder;
        private bool isExportDetailVisible = false;

        private ExportOrderDto newExportOrder = new();
        private ExportOrderDetailDto tempExportDetail = new();
        private List<SparePartItemDto> availableCodedItemsForSelectedPart = new();
        private bool isExportCreateVisible = false;

        private int _selectedExportPartId;
        private int selectedExportPartId
        {
            get => _selectedExportPartId;
            set
            {
                if (_selectedExportPartId != value)
                {
                    _selectedExportPartId = value;
                    _ = OnExportPartSelected(value);
                }
            }
        }

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
            await LoadExportOrders();
        }

        private async void OnFactoryChanged()
        {
            exportPage = 1;
            await LoadExportOrders();
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
                var facId = FactoryState.SelectedFacId;
                var url = "api/SparePart/movement-types";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";
                MovementTypes = await Http.GetFromJsonAsync<List<MovementTypeDto>>(url) ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading lookup data: {ex.Message}");
            }
        }

        private async Task LoadExportOrders()
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = "api/SparePart/export-orders-all";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";

                _allExportOrders = await Http.GetFromJsonAsync<List<ExportOrderDto>>(url) ?? new();
                exportPage = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading exports: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            exportPage = 1;
            StateHasChanged();
        }

        private void OnExportPageChange(PaginationEventArgs args)
        {
            if (exportPageSize != args.PageSize)
            {
                exportPageSize = args.PageSize;
                exportPage = 1;
            }
            else
            {
                exportPage = args.Page;
            }
            StateHasChanged();
        }

        private void ResetFilters()
        {
            exportCodeSearch = "";
            exportMovementTypeFilter = null;
            exportFromDate = null;
            exportToDate = null;
            exportPage = 1;
            StateHasChanged();
        }

        private async Task ShowExportDetail(int exportId)
        {
            selectedExportOrder = await Http.GetFromJsonAsync<ExportOrderDto>($"api/SparePart/export-order/{exportId}");
            isExportDetailVisible = true;
        }

        private async Task ReverseExport(int exportId)
        {
            var conf = await JS.InvokeAsync<bool>("confirm", "Bạn có chắc chắn muốn đảo ngược (Reverse) lệnh xuất kho này? Số lượng tồn kho sẽ được khôi phục về các lô hàng ban đầu.");
            if (!conf) return;

            var res = await Http.PutAsync($"api/SparePart/export-order/{exportId}/status?status=Reversed", null);
            if (res.IsSuccessStatusCode)
            {
                Message.Success("Lệnh xuất đã được đảo ngược thành công.");
                isExportDetailVisible = false;
                await LoadExportOrders();
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                Message.Error($"Không thể đảo ngược: {err}");
            }
        }

        private async Task OpenCreateExportModal()
        {
            newExportOrder = new ExportOrderDto
            {
                ExportDate = DateTime.Now,
                FACID = FactoryState.SelectedFacId ?? CurrentUser.FACID
            };
            tempExportDetail = new ExportOrderDetailDto { Quantity = 1 };
            availableCodedItemsForSelectedPart.Clear();
            selectedExportPartId = 0;
            isExportCreateVisible = true;
            await SearchPartsForExportAsync("");
        }

        private void SearchPartsForExport(string searchText)
        {
            _ = SearchPartsForExportAsync(searchText);
        }

        private async Task SearchPartsForExportAsync(string searchText)
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = $"api/SparePart/get-paged?page=1&pageSize=30&searchText={Uri.EscapeDataString(searchText)}";
                if (facId.HasValue) url += $"&factoryId={facId.Value}";
                
                var res = await Http.GetFromJsonAsync<SparePartPagedResultDto>(url);
                if (res != null)
                {
                    exportPartsSearchList = res.Items ?? new();
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching parts for export: {ex.Message}");
            }
        }

        private async Task OnExportPartSelected(int spid)
        {
            var part = exportPartsSearchList.FirstOrDefault(p => p.SPID == spid);
            if (part != null)
            {
                tempExportDetail.SPID = part.SPID;
                tempExportDetail.PartCode = part.PartCode;
                tempExportDetail.PartName = part.PartName;
                tempExportDetail.HasCode = part.IsCoded;
                if (part.IsCoded)
                {
                    tempExportDetail.Quantity = 1;
                    var itemsRes = await Http.GetFromJsonAsync<PagedResultDto<SparePartItemDto>>($"api/SparePart/coded-items?page=1&pageSize=100&partCode={part.PartCode}&status=Available");
                    if (itemsRes != null)
                    {
                        availableCodedItemsForSelectedPart = itemsRes.Items ?? new();
                    }
                }
                else
                {
                    availableCodedItemsForSelectedPart.Clear();
                }
                await InvokeAsync(StateHasChanged);
            }
        }

        private void AddItemToNewExport()
        {
            if (tempExportDetail.SPID <= 0)
            {
                Message.Error("Vui lòng chọn phụ tùng trước.");
                return;
            }
            if (tempExportDetail.HasCode && string.IsNullOrWhiteSpace(tempExportDetail.SerialCode))
            {
                Message.Error("Vui lòng chọn mã Serial.");
                return;
            }
            if (!tempExportDetail.HasCode && tempExportDetail.Quantity <= 0)
            {
                Message.Error("Số lượng xuất phải lớn hơn 0.");
                return;
            }

            newExportOrder.Details.Add(new ExportOrderDetailDto
            {
                SPID = tempExportDetail.SPID,
                PartCode = tempExportDetail.PartCode,
                PartName = tempExportDetail.PartName,
                HasCode = tempExportDetail.HasCode,
                SerialCode = tempExportDetail.SerialCode,
                Quantity = tempExportDetail.HasCode ? 1 : tempExportDetail.Quantity
            });

            tempExportDetail = new ExportOrderDetailDto { Quantity = 1 };
            availableCodedItemsForSelectedPart.Clear();
            selectedExportPartId = 0;
        }

        private void RemoveExportDetail(ExportOrderDetailDto item)
        {
            newExportOrder.Details.Remove(item);
        }

        private void OnExportAttachmentUploaded(UploadInfo fileinfo)
        {
            if (fileinfo.File.State == UploadState.Success)
            {
                var url = fileinfo.File.Response?.ToString();
                if (!string.IsNullOrEmpty(url))
                {
                    newExportOrder.AttachmentUrls.Add(url);
                    Message.Success($"Đã tải lên tệp đính kèm thành công.");
                }
            }
        }

        private async Task SaveNewExportOrder()
        {
            if (!newExportOrder.Details.Any())
            {
                Message.Error("Lệnh xuất kho phải chứa ít nhất một phụ tùng.");
                return;
            }
            if (!newExportOrder.MovementTypeID.HasValue || newExportOrder.MovementTypeID <= 0)
            {
                Message.Error("Vui lòng chọn hình thức xuất (Movement Type).");
                return;
            }

            var res = await Http.PostAsJsonAsync("api/SparePart/export-order", newExportOrder);
            if (res.IsSuccessStatusCode)
            {
                Message.Success("Tạo lệnh xuất kho thành công.");
                isExportCreateVisible = false;
                await LoadExportOrders();
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                Message.Error($"Lỗi tạo lệnh xuất: {err}");
            }
        }
    }
}
