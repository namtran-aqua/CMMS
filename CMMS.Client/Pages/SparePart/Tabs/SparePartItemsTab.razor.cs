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
    public partial class SparePartItemsTab : ComponentBase, IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }

        [Parameter]
        public bool? IsCoded { get; set; }

        private bool IsAuthenticated { get; set; } = false;
        private UserDto CurrentUser { get; set; } = new();

        private List<SparePartItemDto> _allItems = new();
        private int currentPage = 1;
        private int pageSize = 10;
        private string serialSearch = "";
        private string partCodeSearch = "";
        private string partNameSearch = "";
        private string statusFilter = "";
        private bool isSearchPanelCollapsed = true;

        private List<SparePartItemDto> FilteredItems
        {
            get
            {
                var result = _allItems.AsEnumerable();

                // Filter by Factory
                if (FactoryState.SelectedFacId.HasValue)
                {
                    result = result.Where(x => x.FACID == FactoryState.SelectedFacId.Value);
                }
                if (FactoryState.SelectedDeptId.HasValue)
                {
                    result = result.Where(x => x.DeptID == FactoryState.SelectedDeptId.Value);
                }

                // Filter by serial code search
                if (!string.IsNullOrWhiteSpace(serialSearch))
                {
                    var search = serialSearch.Trim().ToLower();
                    result = result.Where(x => x.SerialCode != null && x.SerialCode.ToLower().Contains(search));
                }

                // Filter by part code search
                if (!string.IsNullOrWhiteSpace(partCodeSearch))
                {
                    var search = partCodeSearch.Trim().ToLower();
                    result = result.Where(x => x.PartCode != null && x.PartCode.ToLower().Contains(search));
                }

                // Filter by part name search
                if (!string.IsNullOrWhiteSpace(partNameSearch))
                {
                    var search = partNameSearch.Trim().ToLower();
                    result = result.Where(x => x.PartName != null && x.PartName.ToLower().Contains(search));
                }

                // Filter by status
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    result = result.Where(x => string.Equals(x.Status, statusFilter, StringComparison.OrdinalIgnoreCase));
                }

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
            await LoadItems();
        }

        protected override async Task OnParametersSetAsync()
        {
            await LoadItems();
        }

        private async void OnFactoryChanged()
        {
            currentPage = 1;
            await LoadItems();
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            FactoryState.OnChange -= OnFactoryChanged;
        }

        private async Task LoadItems()
        {
            try
            {
                var url = "api/SparePart/items-all?";
                
                var queryParams = new List<string>();
                if (FactoryState.SelectedFacId.HasValue)
                    queryParams.Add($"factoryId={FactoryState.SelectedFacId.Value}");
                
                if (IsCoded.HasValue)
                    queryParams.Add($"isCoded={IsCoded.Value.ToString().ToLower()}");

                url += string.Join("&", queryParams);

                _allItems = await Http.GetFromJsonAsync<List<SparePartItemDto>>(url) ?? new();
                currentPage = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading items: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            currentPage = 1;
            StateHasChanged();
        }

        private void OnPageChange(PaginationEventArgs args)
        {
            if (pageSize != args.PageSize)
            {
                pageSize = args.PageSize;
                currentPage = 1;
            }
            else
            {
                currentPage = args.Page;
            }
            StateHasChanged();
        }

        private void ResetFilters()
        {
            serialSearch = "";
            partCodeSearch = "";
            partNameSearch = "";
            statusFilter = "";
            currentPage = 1;
            StateHasChanged();
        }
    }
}
