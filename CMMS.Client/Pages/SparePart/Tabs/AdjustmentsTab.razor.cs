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
    public partial class AdjustmentsTab : ComponentBase, IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        [Inject] private IMessageService Message { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }

        private List<AdjustOrderDto> _allAdjustOrders = new();
        private List<SparePartDto> adjustPartsSearchList { get; set; } = new();

        private int adjustPage = 1;
        private int adjustPageSize = 10;
        private string adjustCodeSearch = "";
        private DateTime? adjustFromDate = null;
        private DateTime? adjustToDate = null;
        private bool isSearchPanelCollapsed = true;

        private List<AdjustOrderDto> FilteredAdjustOrders
        {
            get
            {
                var result = _allAdjustOrders.AsEnumerable();

                // Filter by Factory
                if (FactoryState.SelectedFacId.HasValue)
                {
                    result = result.Where(x => x.FACID == FactoryState.SelectedFacId.Value);
                }

                // Filter by adjust code search
                if (!string.IsNullOrWhiteSpace(adjustCodeSearch))
                {
                    var search = adjustCodeSearch.Trim().ToLower();
                    result = result.Where(x => x.AdjustCode != null && x.AdjustCode.ToLower().Contains(search));
                }

                // Filter by from date
                if (adjustFromDate.HasValue)
                {
                    result = result.Where(x => x.AdjustDate.Date >= adjustFromDate.Value.Date);
                }

                // Filter by to date
                if (adjustToDate.HasValue)
                {
                    result = result.Where(x => x.AdjustDate.Date <= adjustToDate.Value.Date);
                }

                return result.ToList();
            }
        }

        private AdjustOrderDto? selectedAdjustOrder;
        private bool isAdjustDetailVisible = false;

        private CreateAdjustOrderDto newAdjustOrder = new();
        private AdjustOrderDetailDto tempAdjustDetail = new();
        private List<SparePartItemDto> availableCodedItemsForSelectedPart = new();
        private bool isAdjustCreateVisible = false;

        private int _selectedAdjustPartId;
        private int selectedAdjustPartId
        {
            get => _selectedAdjustPartId;
            set
            {
                if (_selectedAdjustPartId != value)
                {
                    _selectedAdjustPartId = value;
                    _ = OnAdjustPartSelected(value);
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
            await LoadAdjustOrders();
        }

        private async void OnFactoryChanged()
        {
            adjustPage = 1;
            await LoadAdjustOrders();
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            FactoryState.OnChange -= OnFactoryChanged;
        }

        private async Task LoadAdjustOrders()
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = "api/SparePart/adjust-orders-all";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";

                _allAdjustOrders = await Http.GetFromJsonAsync<List<AdjustOrderDto>>(url) ?? new();
                adjustPage = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading adjust orders: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            adjustPage = 1;
            StateHasChanged();
        }

        private void OnAdjustPageChange(PaginationEventArgs args)
        {
            if (adjustPageSize != args.PageSize)
            {
                adjustPageSize = args.PageSize;
                adjustPage = 1;
            }
            else
            {
                adjustPage = args.Page;
            }
            StateHasChanged();
        }

        private void ResetFilters()
        {
            adjustCodeSearch = "";
            adjustFromDate = null;
            adjustToDate = null;
            adjustPage = 1;
            StateHasChanged();
        }

        private async Task ShowAdjustDetail(int adjustId)
        {
            selectedAdjustOrder = await Http.GetFromJsonAsync<AdjustOrderDto>($"api/SparePart/adjust-order/{adjustId}");
            isAdjustDetailVisible = true;
        }

        private async Task OpenCreateAdjustModal()
        {
            newAdjustOrder = new CreateAdjustOrderDto
            {
                AdjustDate = DateTime.Now,
                FACID = FactoryState.SelectedFacId ?? CurrentUser.FACID
            };
            tempAdjustDetail = new AdjustOrderDetailDto { Quantity = 1, Type = "IN" };
            availableCodedItemsForSelectedPart.Clear();
            selectedAdjustPartId = 0;
            isAdjustCreateVisible = true;
            await SearchPartsForAdjustAsync("");
        }

        private void SearchPartsForAdjust(string searchText)
        {
            _ = SearchPartsForAdjustAsync(searchText);
        }

        private async Task SearchPartsForAdjustAsync(string searchText)
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = $"api/SparePart/get-paged?page=1&pageSize=30&searchText={Uri.EscapeDataString(searchText)}";
                if (facId.HasValue) url += $"&factoryId={facId.Value}";
                
                var res = await Http.GetFromJsonAsync<SparePartPagedResultDto>(url);
                if (res != null)
                {
                    adjustPartsSearchList = res.Items ?? new();
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching parts for adjustment: {ex.Message}");
            }
        }

        private async Task OnAdjustPartSelected(int spid)
        {
            var part = adjustPartsSearchList.FirstOrDefault(p => p.SPID == spid);
            if (part != null)
            {
                tempAdjustDetail.SPID = part.SPID;
                tempAdjustDetail.PartCode = part.PartCode;
                tempAdjustDetail.PartName = part.PartName;
                tempAdjustDetail.HasCode = part.IsCoded;
                if (part.IsCoded)
                {
                    tempAdjustDetail.Quantity = 1;
                    if (tempAdjustDetail.Type == "OUT")
                    {
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
                }
                else
                {
                    availableCodedItemsForSelectedPart.Clear();
                }
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task OnAdjustTypeChanged(string newType)
        {
            tempAdjustDetail.Type = newType;
            if (tempAdjustDetail.HasCode && newType == "OUT" && tempAdjustDetail.SPID > 0)
            {
                var part = adjustPartsSearchList.FirstOrDefault(p => p.SPID == tempAdjustDetail.SPID);
                if (part != null)
                {
                    var itemsRes = await Http.GetFromJsonAsync<PagedResultDto<SparePartItemDto>>($"api/SparePart/coded-items?page=1&pageSize=100&partCode={part.PartCode}&status=Available");
                    if (itemsRes != null)
                    {
                        availableCodedItemsForSelectedPart = itemsRes.Items ?? new();
                    }
                }
            }
            else
            {
                availableCodedItemsForSelectedPart.Clear();
            }
            await InvokeAsync(StateHasChanged);
        }

        private void AddItemToNewAdjust()
        {
            if (tempAdjustDetail.SPID <= 0)
            {
                Message.Error("Vui lòng chọn phụ tùng trước.");
                return;
            }
            if (tempAdjustDetail.HasCode && string.IsNullOrWhiteSpace(tempAdjustDetail.SerialCode))
            {
                if (tempAdjustDetail.Type == "IN")
                {
                    Message.Error("Vui lòng nhập mã Serial mới.");
                }
                else
                {
                    Message.Error("Vui lòng chọn mã Serial từ kho.");
                }
                return;
            }
            if (!tempAdjustDetail.HasCode && tempAdjustDetail.Quantity <= 0)
            {
                Message.Error("Số lượng điều chỉnh phải lớn hơn 0.");
                return;
            }

            newAdjustOrder.Lines.Add(new AdjustOrderDetailDto
            {
                SPID = tempAdjustDetail.SPID,
                PartCode = tempAdjustDetail.PartCode,
                PartName = tempAdjustDetail.PartName,
                HasCode = tempAdjustDetail.HasCode,
                SerialCode = tempAdjustDetail.SerialCode,
                Type = tempAdjustDetail.Type,
                Quantity = tempAdjustDetail.HasCode ? 1 : tempAdjustDetail.Quantity
            });

            tempAdjustDetail = new AdjustOrderDetailDto { Quantity = 1, Type = "IN" };
            availableCodedItemsForSelectedPart.Clear();
            selectedAdjustPartId = 0;
        }

        private void RemoveAdjustDetail(AdjustOrderDetailDto item)
        {
            newAdjustOrder.Lines.Remove(item);
        }

        private void OnAdjustAttachmentUploaded(UploadInfo fileinfo)
        {
            if (fileinfo.File.State == UploadState.Success)
            {
                var url = fileinfo.File.Response?.ToString();
                if (!string.IsNullOrEmpty(url))
                {
                    newAdjustOrder.AttachmentUrls.Add(url);
                    Message.Success($"Đã tải lên tệp đính kèm thành công.");
                }
            }
        }

        private async Task SaveNewAdjustOrder()
        {
            if (!newAdjustOrder.Lines.Any())
            {
                Message.Error("Lệnh điều chỉnh phải chứa ít nhất một phụ tùng.");
                return;
            }

            var res = await Http.PostAsJsonAsync("api/SparePart/create-adjust-order", newAdjustOrder);
            if (res.IsSuccessStatusCode)
            {
                Message.Success("Tạo lệnh điều chỉnh thành công.");
                isAdjustCreateVisible = false;
                await LoadAdjustOrders();
            }
            else
            {
                var errRes = await res.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                var errMsg = errRes != null && errRes.ContainsKey("message") ? errRes["message"] : "Không rõ lỗi";
                Message.Error($"Lỗi tạo lệnh điều chỉnh: {errMsg}");
            }
        }
    }
}
