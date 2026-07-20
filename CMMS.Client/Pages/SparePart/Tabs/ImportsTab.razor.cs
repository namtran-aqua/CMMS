using CMMS.Client.Services;
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
    public partial class ImportsTab : ComponentBase, IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        [Inject] private IMessageService Message { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }

        private List<ImportOrderDto> _allImportOrders = new();
        private List<VendorDto> Vendors { get; set; } = new();
        private List<SparePartDto> importPartsSearchList { get; set; } = new();

        private int importPage = 1;
        private int importPageSize = 10;
        private string importCodeSearch = "";
        private string importPoSearch = "";
        private int? importVendorFilter = 0;
        private DateTime? importFromDate = null;
        private DateTime? importToDate = null;
        private bool isCatalogSearchPanelCollapsed = true;

        private List<ImportOrderDto> FilteredImportOrders
        {
            get
            {
                var result = _allImportOrders.AsEnumerable();

                // Filter by Factory
                if (FactoryState.SelectedFacId.HasValue)
                {
                    result = result.Where(x => x.FACID == FactoryState.SelectedFacId.Value);
                }

                // Filter by import code search
                if (!string.IsNullOrWhiteSpace(importCodeSearch))
                {
                    var search = importCodeSearch.Trim().ToLower();
                    result = result.Where(x => x.ImportCode != null && x.ImportCode.ToLower().Contains(search));
                }

                // Filter by PO number search
                if (!string.IsNullOrWhiteSpace(importPoSearch))
                {
                    var search = importPoSearch.Trim().ToLower();
                    result = result.Where(x => x.PONumber != null && x.PONumber.ToLower().Contains(search));
                }

                // Filter by vendor
                if (importVendorFilter > 0)
                {
                    result = result.Where(x => x.VendorID == importVendorFilter);
                }

                // Filter by from date
                if (importFromDate.HasValue)
                {
                    result = result.Where(x => x.ImportDate.Date >= importFromDate.Value.Date);
                }

                // Filter by to date
                if (importToDate.HasValue)
                {
                    result = result.Where(x => x.ImportDate.Date <= importToDate.Value.Date);
                }

                return result.ToList();
            }
        }

        private ImportOrderDto? selectedImportOrder;
        private bool isImportDetailVisible = false;

        private ImportOrderDto newImportOrder = new();
        private ImportOrderDetailDto tempImportDetail = new();
        private bool isImportCreateVisible = false;

        private int _selectedImportPartId;
        private int selectedImportPartId
        {
            get => _selectedImportPartId;
            set
            {
                if (_selectedImportPartId != value)
                {
                    _selectedImportPartId = value;
                    _ = OnImportPartSelected(value);
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
            await LoadImportOrders();
        }

        private async void OnFactoryChanged()
        {
            importPage = 1;
            await LoadImportOrders();
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
                Vendors = await Http.GetFromJsonAsync<List<VendorDto>>("api/vendor/vendors") ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading lookup data: {ex.Message}");
            }
        }

        private async Task LoadImportOrders()
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = "api/SparePart/import-orders-all";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";

                _allImportOrders = await Http.GetFromJsonAsync<List<ImportOrderDto>>(url) ?? new();
                importPage = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading imports: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            importPage = 1;
            StateHasChanged();
        }

        private void OnImportPageChange(PaginationEventArgs args)
        {
            if (importPageSize != args.PageSize)
            {
                importPageSize = args.PageSize;
                importPage = 1;
            }
            else
            {
                importPage = args.Page;
            }
            StateHasChanged();
        }

        private void ResetFilters()
        {
            importCodeSearch = "";
            importPoSearch = "";
            importVendorFilter = 0;
            importFromDate = null;
            importToDate = null;
            importPage = 1;
            StateHasChanged();
        }

        private async Task ShowImportDetail(int importId)
        {
            selectedImportOrder = await Http.GetFromJsonAsync<ImportOrderDto>($"api/SparePart/import-order/{importId}");
            isImportDetailVisible = true;
        }

        private async Task ReverseImport(int importId)
        {
            var conf = await JS.InvokeAsync<bool>("confirm", "Bạn có chắc chắn muốn đảo ngược (Reverse) lệnh nhập kho này? Số lượng tồn kho sẽ được hoàn trả lại và các mặt hàng sẽ chuyển sang Returned.");
            if (!conf) return;

            var res = await Http.PutAsync($"api/SparePart/import-order/{importId}/status?status=Reversed", null);
            if (res.IsSuccessStatusCode)
            {
                Message.Success("Lệnh nhập đã được đảo ngược thành công.");
                isImportDetailVisible = false;
                await LoadImportOrders();
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                Message.Error($"Không thể đảo ngược: {err}");
            }
        }

        private async Task OpenCreateImportModal()
        {
            newImportOrder = new ImportOrderDto
            {
                ImportDate = DateTime.Now,
                FACID = FactoryState.SelectedFacId ?? CurrentUser.FACID
            };
            tempImportDetail = new ImportOrderDetailDto { Quantity = 1 };
            selectedImportPartId = 0;
            isImportCreateVisible = true;
            await SearchPartsForImportAsync("");
        }

        private void SearchPartsForImport(string searchText)
        {
            _ = SearchPartsForImportAsync(searchText);
        }

        private async Task SearchPartsForImportAsync(string searchText)
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = $"api/SparePart/get-paged?page=1&pageSize=30&searchText={Uri.EscapeDataString(searchText)}";
                if (facId.HasValue) url += $"&factoryId={facId.Value}";
                
                var res = await Http.GetFromJsonAsync<SparePartPagedResultDto>(url);
                if (res != null)
                {
                    importPartsSearchList = res.Items ?? new();
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching parts for import: {ex.Message}");
            }
        }

        private async Task OnImportPartSelected(int spid)
        {
            var part = importPartsSearchList.FirstOrDefault(p => p.SPID == spid);
            if (part == null && spid > 0)
            {
                part = await Http.GetFromJsonAsync<SparePartDto>($"api/SparePart/get-paged?page=1&pageSize=1&searchText={spid}");
            }

            if (part != null)
            {
                tempImportDetail.SPID = part.SPID;
                tempImportDetail.PartCode = part.PartCode;
                tempImportDetail.PartName = part.PartName;
                tempImportDetail.HasCode = part.IsCoded;
                tempImportDetail.Price = part.Price ?? 0m;
                if (part.IsCoded)
                {
                    tempImportDetail.Quantity = 1;
                }
                await InvokeAsync(StateHasChanged);
            }
        }

        private void AddItemToNewImport()
        {
            if (tempImportDetail.SPID <= 0)
            {
                Message.Error("Vui lòng chọn phụ tùng trước.");
                return;
            }
            if (tempImportDetail.HasCode && string.IsNullOrWhiteSpace(tempImportDetail.SerialCode))
            {
                Message.Error("Phụ tùng này theo dõi theo mã nên bắt đầu nhập Serial Code.");
                return;
            }
            if (!tempImportDetail.HasCode && tempImportDetail.Quantity <= 0)
            {
                Message.Error("Số lượng nhập phải lớn hơn 0.");
                return;
            }

            if (tempImportDetail.HasCode && newImportOrder.Details.Any(d => d.SerialCode == tempImportDetail.SerialCode))
            {
                Message.Error("Mã Serial này đã được thêm trong lệnh hiện tại.");
                return;
            }

            newImportOrder.Details.Add(new ImportOrderDetailDto
            {
                SPID = tempImportDetail.SPID,
                PartCode = tempImportDetail.PartCode,
                PartName = tempImportDetail.PartName,
                HasCode = tempImportDetail.HasCode,
                SerialCode = tempImportDetail.SerialCode,
                Quantity = tempImportDetail.HasCode ? 1 : tempImportDetail.Quantity,
                Price = tempImportDetail.Price
            });

            tempImportDetail = new ImportOrderDetailDto { Quantity = 1 };
            selectedImportPartId = 0;
        }

        private void RemoveImportDetail(ImportOrderDetailDto item)
        {
            newImportOrder.Details.Remove(item);
        }

        private void OnImportAttachmentUploaded(UploadInfo fileinfo)
        {
            if (fileinfo.File.State == UploadState.Success)
            {
                var url = fileinfo.File.Response?.ToString();
                if (!string.IsNullOrEmpty(url))
                {
                    newImportOrder.AttachmentUrls.Add(url);
                    Message.Success($"Đã tải lên tệp đính kèm thành công.");
                }
            }
        }

        private async Task SaveNewImportOrder()
        {
            if (!newImportOrder.Details.Any())
            {
                Message.Error("Lệnh nhập kho phải chứa ít nhất một phụ tùng.");
                return;
            }

            var res = await Http.PostAsJsonAsync("api/SparePart/import-order", newImportOrder);
            if (res.IsSuccessStatusCode)
            {
                Message.Success("Tạo lệnh nhập kho thành công.");
                isImportCreateVisible = false;
                await LoadImportOrders();
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                Message.Error($"Lỗi tạo lệnh nhập: {err}");
            }
        }
    }
}
