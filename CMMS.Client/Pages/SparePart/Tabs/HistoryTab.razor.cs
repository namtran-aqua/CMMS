using CMMS.Client.Services;
using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.Common;
using Microsoft.AspNetCore.Components;
using AntDesign;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using CMMS.Shared.Dtos.User;
using CMMS.Client.Common;
using Microsoft.AspNetCore.Components.Authorization;

namespace CMMS.Client.Pages.SpareParts.Tabs
{
    public partial class HistoryTab : ComponentBase, IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }

        private bool IsAuthenticated { get; set; } = false;
        private UserDto CurrentUser { get; set; } = new();

        private List<SparePartTransactionDto> _allTransactions = new();
        private int historyPage = 1;
        private int historyPageSize = 10;
        private string historySearchText = "";
        private string historyTypeFilter = "";
        private DateTime? historyFromDate = null;
        private DateTime? historyToDate = null;
        private bool isCatalogSearchPanelCollapsed = true;

        private List<SparePartTransactionDto> FilteredTransactions
        {
            get
            {
                var result = _allTransactions.AsEnumerable();

                // Filter by Factory
                if (FactoryState.SelectedFacId.HasValue)
                {
                    result = result.Where(x => x.FACID == FactoryState.SelectedFacId.Value);
                }

                // Filter by Search Text
                if (!string.IsNullOrWhiteSpace(historySearchText))
                {
                    var search = historySearchText.Trim().ToLower();
                    result = result.Where(x =>
                        (x.PartCode != null && x.PartCode.ToLower().Contains(search)) ||
                        (x.PartName != null && x.PartName.ToLower().Contains(search)) ||
                        (x.RefCode != null && x.RefCode.ToLower().Contains(search)) ||
                        (x.Note != null && x.Note.ToLower().Contains(search)) ||
                        (x.Equipment != null && x.Equipment.ToLower().Contains(search))
                    );
                }

                // Filter by type filter
                if (!string.IsNullOrWhiteSpace(historyTypeFilter))
                {
                    result = result.Where(x => string.Equals(x.Type, historyTypeFilter, StringComparison.OrdinalIgnoreCase));
                }

                // Filter by from date
                if (historyFromDate.HasValue)
                {
                    result = result.Where(x => x.Date.Date >= historyFromDate.Value.Date);
                }

                // Filter by to date
                if (historyToDate.HasValue)
                {
                    result = result.Where(x => x.Date.Date <= historyToDate.Value.Date);
                }

                // Sort by date descending
                result = result.OrderByDescending(x => x.Date).ThenByDescending(x => x.TransID);

                return result.ToList();
            }
        }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
            CurrentUser = await CurrentUserClass.LoadCurrentUser();

            FactoryState.OnChange += OnFactoryChanged;
            await LoadHistory();
        }

        private async void OnFactoryChanged()
        {
            historyPage = 1;
            await LoadHistory();
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            FactoryState.OnChange -= OnFactoryChanged;
        }

        private async Task LoadHistory()
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = "api/SparePart/history";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";

                _allTransactions = await Http.GetFromJsonAsync<List<SparePartTransactionDto>>(url) ?? new();
                historyPage = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading history log: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            historyPage = 1;
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

        private void ResetFilters()
        {
            historySearchText = "";
            historyTypeFilter = "";
            historyFromDate = null;
            historyToDate = null;
            historyPage = 1;
            StateHasChanged();
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
