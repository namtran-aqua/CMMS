using AntDesign;
using CMMS.Client.Common;
using CMMS.Client.Components;
using CMMS.Client.Components.Equipments;
using CMMS.Client.Services;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Json;
namespace CMMS.Client.Pages.Equipment
{
    public partial class Equipment : IDisposable
    {
        #region Declaration
        [Inject] private HttpClient Http { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        private bool IsAuthenticated { get; set; } = false;

        private UserDto CurrentUser { get; set; } = new();
        private List<EquipmentDto> _equipments = new();
        private EquipmentModal? equipmentModal;
        private ImportModal? importModal;

        private int currentPage = 1;
        private int pageSize = 10;

        private string _searchText = "";
        private string searchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    currentPage = 1;
                }
            }
        }

        private string _selectedStatus = "All Status";
        private string selectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (_selectedStatus != value)
                {
                    _selectedStatus = value;
                    currentPage = 1;
                }
            }
        }

        private string _sortBy = "NextMaintAsc";
        private string sortBy
        {
            get => _sortBy;
            set
            {
                if (_sortBy != value)
                {
                    _sortBy = value;
                    currentPage = 1;
                }
            }
        }

        private void OnPageChange(PaginationEventArgs args)
        {
            if (pageSize != args.PageSize)
            {
                pageSize = args.PageSize;
                currentPage = 1; // đổi page size thì về lại trang đầu
            }
            else
            {
                currentPage = args.Page;
            }
            StateHasChanged();
        }
        private bool isUpdating;

        private List<EquipmentDto> FilteredEquipments
        {
            get
            {
                var result = _equipments.AsEnumerable();

                // Filter by Factory
                if (FactoryState.SelectedFacId.HasValue)
                {
                    result = result.Where(e => e.FACID == FactoryState.SelectedFacId.Value);
                }

                // Filter by Search Text
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var search = searchText.Trim().ToLower();
                    result = result.Where(e =>
                        (e.EquipmentName != null && e.EquipmentName.ToLower().Contains(search)) ||
                        (e.EquipmentCode != null && e.EquipmentCode.ToLower().Contains(search)) ||
                        (e.EquipmentModel != null && e.EquipmentModel.ToLower().Contains(search)) ||
                        (e.EquipmentSerial != null && e.EquipmentSerial.ToLower().Contains(search)) ||
                        (e.LocName != null && e.LocName.ToLower().Contains(search))
                    );
                }

                // Filter by Status
                if (selectedStatus != "All Status" && !string.IsNullOrWhiteSpace(selectedStatus))
                {
                    result = result.Where(e => string.Equals(e.StsUseName, selectedStatus, StringComparison.OrdinalIgnoreCase));
                }

                // Sort
                result = sortBy switch
                {
                    "NextMaintAsc" => result.OrderBy(e => e.NextMaintenanceDate ?? DateTime.MaxValue),
                    "NextMaintDesc"=> result.OrderByDescending(e => e.NextMaintenanceDate ?? DateTime.MinValue),
                    "NameAsc"      => result.OrderBy(e => e.EquipmentName ?? ""),
                    "NameDesc"     => result.OrderByDescending(e => e.EquipmentName ?? ""),
                    "StatusAsc"    => result.OrderBy(e => e.StsUseName ?? ""),
                    _              => result.OrderBy(e => e.NextMaintenanceDate ?? DateTime.MaxValue)
                };

                return result.ToList();
            }
        }
        #endregion

        #region Innit
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
            currentPage = 1;
            await LoadData();
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
                var facId = FactoryState.SelectedFacId;
                var url = "api/equipment/get-all";
                if (facId.HasValue)
                {
                    url += $"?factoryId={facId.Value}";
                }
                var res = await Http.GetFromJsonAsync<List<EquipmentDto>>(url);
                _equipments = res ?? new List<EquipmentDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading equipment: {ex.Message}");
            }

        }
        #endregion
        #region Action
        //private string GetStsUseNameClass(string? StsUseName)
        //{
        //    return StsUseName switch
        //    {
        //        "Running" => "badge bg-dark",
        //        "Fault" => "badge bg-danger",
        //        "Maintenance" => "badge bg-secondary",
        //        "Stopped" => "badge bg-light text-dark",
        //        _ => "badge bg-secondary"
        //    };
        //}
        private (string text, string color) GetMaintenanceText(DateTime? date)
        {
            if (date == null)
                return ("N/A", "black");

            var today = DateTime.Now.Date;
            var diff = (date.Value.Date - today).Days;

            if (diff > 0)
            {
                if (diff <= 7)
                    return ($"In {diff} days", "orange");

                return ($"In {diff} days", "black");
            }
            else if (diff < 0)
            {
                return ($"{Math.Abs(diff)} days overdue", "red");
            }
            else
            {
                return ("Today", "orange");
            }
        }
        private void ShowImportModal()
        {
            if (importModal != null)
                importModal.Show();
        }

        private async Task CreatedAsync()
        {
            if(equipmentModal != null)
            {
                await equipmentModal.ShowModal(false);
            }
        }
        private async Task TaskUpdate()
        {
            var response = await Http.PostAsync("api/equipment/update-status",null);

            if (response.IsSuccessStatusCode)
            {
                Message.Success("Cập nhật trạng thái thành công");
                await LoadData();
            }
        }
        private async Task EditAsync(EquipmentDto equipmentDto)
        {
            if (equipmentModal != null)
            {
                await equipmentModal.ShowModal(true, equipmentDto);
            }
        }
        private async Task DeleteAsync(int id)
        {
            var confirm = await JS.InvokeAsync<bool>("confirm", "Bạn có chắc chắn muốn xóa không?");
            if (!confirm)
                return;
            var response = await Http.DeleteAsync($"api/equipment/delete/{id}");
            if (response.IsSuccessStatusCode)
            {
                Message.Success("Xóa thành công !");
            }
            else
            {
                Message.Error("Xóa thất bại !");
            }
            await LoadData();
            await InvokeAsync(StateHasChanged);
        }
        #endregion
    }
}
