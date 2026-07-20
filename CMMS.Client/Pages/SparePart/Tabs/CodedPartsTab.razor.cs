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
    public partial class CodedPartsTab : ComponentBase, IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }

        private bool IsAuthenticated { get; set; } = false;
        private UserDto CurrentUser { get; set; } = new();

        private List<SparePartItemDto> _allCodedItems = new();
        private int codedPage = 1;
        private int codedPageSize = 10;
        private string codedSerialSearch = "";
        private string codedPartCodeSearch = "";
        private string codedPartNameSearch = "";
        private string codedStatusFilter = "";
        private bool isSearchPanelCollapsed = true;

        private List<SparePartItemDto> FilteredCodedItems
        {
            get
            {
                var result = _allCodedItems.AsEnumerable();

                // Filter by Factory
                if (FactoryState.SelectedFacId.HasValue)
                {
                    result = result.Where(x => x.FACID == FactoryState.SelectedFacId.Value);
                }

                // Filter by serial code search
                if (!string.IsNullOrWhiteSpace(codedSerialSearch))
                {
                    var search = codedSerialSearch.Trim().ToLower();
                    result = result.Where(x => x.SerialCode != null && x.SerialCode.ToLower().Contains(search));
                }

                // Filter by part code search
                if (!string.IsNullOrWhiteSpace(codedPartCodeSearch))
                {
                    var search = codedPartCodeSearch.Trim().ToLower();
                    result = result.Where(x => x.PartCode != null && x.PartCode.ToLower().Contains(search));
                }

                // Filter by part name search
                if (!string.IsNullOrWhiteSpace(codedPartNameSearch))
                {
                    var search = codedPartNameSearch.Trim().ToLower();
                    result = result.Where(x => x.PartName != null && x.PartName.ToLower().Contains(search));
                }

                // Filter by status
                if (!string.IsNullOrWhiteSpace(codedStatusFilter))
                {
                    result = result.Where(x => string.Equals(x.Status, codedStatusFilter, StringComparison.OrdinalIgnoreCase));
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
            await LoadCodedParts();
        }

        private async void OnFactoryChanged()
        {
            codedPage = 1;
            await LoadCodedParts();
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            FactoryState.OnChange -= OnFactoryChanged;
        }

        private async Task LoadCodedParts()
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = "api/SparePart/coded-items-all";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";

                _allCodedItems = await Http.GetFromJsonAsync<List<SparePartItemDto>>(url) ?? new();
                codedPage = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading coded parts: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            codedPage = 1;
            StateHasChanged();
        }

        private void OnCodedPageChange(PaginationEventArgs args)
        {
            if (codedPageSize != args.PageSize)
            {
                codedPageSize = args.PageSize;
                codedPage = 1;
            }
            else
            {
                codedPage = args.Page;
            }
            StateHasChanged();
        }

        private void ResetFilters()
        {
            codedSerialSearch = "";
            codedPartCodeSearch = "";
            codedPartNameSearch = "";
            codedStatusFilter = "";
            codedPage = 1;
            StateHasChanged();
        }
    }
}
