using AntDesign;
using CMMS.Client.Common;
using CMMS.Client.Components.Equipments;
using CMMS.Client.Modals.Maintenances;
using CMMS.Client.Services;
using CMMS.Shared.Dtos.DashBoards;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Maintenance;
using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.Maintenance
{
    public partial class Maintenance : IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        private bool IsAuthenticated { get; set; } = false;
        private List<EquipmentDto> _equipments = new();
        public List<DashBoarDto> DashBoardData { get; set; } = new();
        private List<DashBoarDto> DueSoon { get; set; } = new();
        private List<DashBoarDto> OverDue { get; set; } = new();
        private List<MaintenanceDto> _maintenances = new();
        private List<MaintenanceDto> MaintenanceGroups = new();
        private MaintenanceModel? _maintenanceModal;
        private MaintenanceDetailModel? _detailModal;
        private Table<MaintenanceModel>? _tableRef;
        private UserDto CurrentUser { get; set; } = new();

        private bool isLoading = true;

        private string selectedTab = "Pending";

        private int pendingPage = 1;
        private int pendingPageSize = 10;
        private int historyPage = 1;
        private int historyPageSize = 10;

        private string _pendingSearchText = "";
        private string pendingSearchText
        {
            get => _pendingSearchText;
            set
            {
                if (_pendingSearchText != value)
                {
                    _pendingSearchText = value;
                    pendingPage = 1;
                }
            }
        }

        private string _pendingSortBy = "DueDateAsc";
        private string pendingSortBy
        {
            get => _pendingSortBy;
            set
            {
                if (_pendingSortBy != value)
                {
                    _pendingSortBy = value;
                    pendingPage = 1;
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

        private int _historyStatusFilter = 0;
        private int historyStatusFilter
        {
            get => _historyStatusFilter;
            set
            {
                if (_historyStatusFilter != value)
                {
                    _historyStatusFilter = value;
                    historyPage = 1;
                }
            }
        }

        private string _historySortBy = "DateDesc";
        private string historySortBy
        {
            get => _historySortBy;
            set
            {
                if (_historySortBy != value)
                {
                    _historySortBy = value;
                    historyPage = 1;
                }
            }
        }

        private string GetEquipmentName(int eqid)
        {
            var eq = DashBoardData?.FirstOrDefault(d => d.EQID == eqid);
            return eq?.EquipmentName ?? $"EQ #{eqid}";
        }

        private string GetStsMainName(int stsMainID)
        {
            return stsMainID switch
            {
                1 => "Routine",
                2 => "Repair",
                _ => "Routine"
            };
        }

        private List<DashBoarDto> FilteredOverDue => FilterAndSortPending(OverDue);
        private List<DashBoarDto> FilteredDueSoon => FilterAndSortPending(DueSoon);

        private List<DashBoarDto> FilteredPendingCombined
        {
            get
            {
                var list = new List<DashBoarDto>();
                list.AddRange(FilteredOverDue);
                list.AddRange(FilteredDueSoon);
                return list;
            }
        }

        private List<DashBoarDto> FilterAndSortPending(List<DashBoarDto> source)
        {
            if (source == null) return new();
            var result = source.AsEnumerable();
            // Filter by Factory
            if (FactoryState.SelectedFacId.HasValue)
            {
                result = result.Where(e => e.FACID == FactoryState.SelectedFacId.Value);
            }

            if (!string.IsNullOrWhiteSpace(pendingSearchText))
            {
                var search = pendingSearchText.Trim().ToLower();
                result = result.Where(x =>
                    (x.EquipmentName != null && x.EquipmentName.ToLower().Contains(search)) ||
                    (x.LocName != null && x.LocName.ToLower().Contains(search)) ||
                    (x.PIC != null && x.PIC.ToLower().Contains(search))
                );
            }

            result = pendingSortBy switch
            {
                "NameAsc" => result.OrderBy(x => x.EquipmentName ?? ""),
                "NameDesc" => result.OrderByDescending(x => x.EquipmentName ?? ""),
                "DueDateAsc" => result.OrderBy(x => x.LastMaintenanceDate.HasValue && x.MaintenanceCircleTime.HasValue ? x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value) : DateTime.MaxValue),
                "DueDateDesc" => result.OrderByDescending(x => x.LastMaintenanceDate.HasValue && x.MaintenanceCircleTime.HasValue ? x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value) : DateTime.MinValue),
                _ => result
            };

            return result.ToList();
        }

        private List<MaintenanceDto> FilteredHistory
        {
            get
            {
                if (_maintenances == null) return new();
                var result = _maintenances.AsEnumerable();

                // Filter by search text
                if (!string.IsNullOrWhiteSpace(historySearchText))
                {
                    var search = historySearchText.Trim().ToLower();
                    result = result.Where(m =>
                        GetEquipmentName(m.EQID).ToLower().Contains(search) ||
                        (m.MaintPIC != null && m.MaintPIC.ToLower().Contains(search)) ||
                        (m.MaintDescription != null && m.MaintDescription.ToLower().Contains(search))
                    );
                }

                // Filter by status
                if (historyStatusFilter > 0)
                {
                    result = result.Where(m => m.StsMainID == historyStatusFilter);
                }

                // Sort
                result = historySortBy switch
                {
                    "DateDesc" => result.OrderByDescending(m => m.MaintDate ?? DateTime.MinValue),
                    "DateAsc" => result.OrderBy(m => m.MaintDate ?? DateTime.MaxValue),
                    "CostDesc" => result.OrderByDescending(m => m.MaintPrice ?? 0),
                    "CostAsc" => result.OrderBy(m => m.MaintPrice ?? 0),
                    "NameAsc" => result.OrderBy(m => GetEquipmentName(m.EQID)),
                    "NameDesc" => result.OrderByDescending(m => GetEquipmentName(m.EQID)),
                    _ => result.OrderByDescending(m => m.MaintDate ?? DateTime.MinValue)
                };

                return result.ToList();
            }
        }
        private async void OnFactoryChanged()
        {
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            FactoryState.OnChange -= OnFactoryChanged;
        }

        protected override async Task OnInitializedAsync()
        {
            //var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
            //CurrentUser = await CurrentUserClass.LoadCurrentUser();
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
            CurrentUser = await CurrentUserClass.LoadCurrentUser();

            // Subscribe factory change event
            FactoryState.OnChange += OnFactoryChanged;

            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                isLoading = true;

                var dashboardTask =
                    Http.GetFromJsonAsync<List<DashBoarDto>>(
                        "api/DashBoard/dashboard");

                var maintenanceTask =
                    Http.GetFromJsonAsync<List<MaintenanceDto>>(
                        "api/Maintenance/get-all");

                await Task.WhenAll(dashboardTask, maintenanceTask);

                DashBoardData = await dashboardTask ?? new();

                _maintenances = await maintenanceTask ?? new();
                MaintenanceGroups = _maintenances
                    .GroupBy(x => x.EQID)
                    .Select(g => new MaintenanceDto
                    {
                        EQID = g.Key,
                        Items = g.ToList()
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        protected override void OnParametersSet()
        {
            if (DashBoardData == null) return;

            var currentDate = DateTime.Today;

            DueSoon = DashBoardData.Where(x =>
            {
                if (x.IsActive == true && x.LastMaintenanceDate.HasValue && x.MaintenanceCircleTime.HasValue)
                {
                    var nextDate = x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value);
                    var diffDays = (nextDate - currentDate).TotalDays;
                    return diffDays >= 0 && diffDays <= 7;
                }
                return false;
            })
            .OrderBy(x => x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value))
            .ToList();

            OverDue = DashBoardData.Where(x =>
            {
                if (x.IsActive == true && x.LastMaintenanceDate.HasValue && x.MaintenanceCircleTime.HasValue)
                {
                    var nextDate = x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value);
                    return nextDate < currentDate;
                }
                return false;
            })
            .OrderBy(x => x.LastMaintenanceDate.Value.AddDays(x.MaintenanceCircleTime.Value))
            .ToList();
        }

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

        private async Task CreatedAsync(int eqid)
        {
            if (_maintenanceModal != null)
            {
                await _maintenanceModal.ShowModal(eqid);
            }
        }

        private void ShowDetail(MaintenanceDto record)
        {
            if (_detailModal != null)
            {
                var eqName = GetEquipmentName(record.EQID);
                _detailModal.Show(record, eqName);
            }
        }

        private void OnPendingPageChange(PaginationEventArgs args)
        {
            if (pendingPageSize != args.PageSize)
            {
                pendingPageSize = args.PageSize;
                pendingPage = 1; 
            }
            else
            {
                pendingPage = args.Page;
            }
            StateHasChanged();
        }

        private void OnHistoryPageChange(PaginationEventArgs args)
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
            StateHasChanged();
        }
    }
}
