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
    public partial class MonthlyClosingTab : ComponentBase, IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }
        [Inject] private IMessageService Message { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }

        private bool IsAuthenticated { get; set; } = false;
        private UserDto CurrentUser { get; set; } = new();

        private List<SparePartMonthlyPeriodDto> _allMonthlyPeriods = new();
        private int closingPage = 1;
        private int closingPageSize = 10;

        private List<SparePartMonthlyPeriodDto> FilteredMonthlyPeriods => _allMonthlyPeriods;

        private bool isClosingCreateVisible = false;
        private int closeYear = DateTime.Now.Year;
        private int closeMonth = DateTime.Now.Month;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
            CurrentUser = await CurrentUserClass.LoadCurrentUser();

            FactoryState.OnChange += OnFactoryChanged;
            await LoadMonthlyPeriods();
        }

        private async void OnFactoryChanged()
        {
            closingPage = 1;
            await LoadMonthlyPeriods();
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            FactoryState.OnChange -= OnFactoryChanged;
        }

        private async Task LoadMonthlyPeriods()
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = "api/SparePart/monthly-periods-all";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";

                _allMonthlyPeriods = await Http.GetFromJsonAsync<List<SparePartMonthlyPeriodDto>>(url) ?? new();
                closingPage = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading monthly periods: {ex.Message}");
            }
        }

        private void OnClosingPageChange(PaginationEventArgs args)
        {
            if (closingPageSize != args.PageSize)
            {
                closingPageSize = args.PageSize;
                closingPage = 1;
            }
            else
            {
                closingPage = args.Page;
            }
            StateHasChanged();
        }

        private async Task SavePeriodClosing()
        {
            var res = await Http.PostAsync($"api/SparePart/close-period?year={closeYear}&month={closeMonth}", null);
            if (res.IsSuccessStatusCode)
            {
                Message.Success("Chốt sổ kỳ kế toán thành công.");
                isClosingCreateVisible = false;
                await LoadMonthlyPeriods();
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                Message.Error($"Lỗi chốt sổ: {err}");
            }
        }
    }
}
