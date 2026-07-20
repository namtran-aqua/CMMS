using CMMS.Client.Services;
using CMMS.Shared.Dtos.SpareParts;
using Microsoft.AspNetCore.Components;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CMMS.Client.Pages.SpareParts.Tabs
{
    public partial class DashboardTab : ComponentBase, IDisposable
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private FactoryStateService FactoryState { get; set; }

        private SparePartDashboardDto DashboardData { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            FactoryState.OnChange += OnFactoryChanged;
            await LoadDashboard();
        }

        private async void OnFactoryChanged()
        {
            await LoadDashboard();
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadDashboard()
        {
            try
            {
                var facId = FactoryState.SelectedFacId;
                var url = "api/SparePart/dashboard";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";

                DashboardData = await Http.GetFromJsonAsync<SparePartDashboardDto>(url) ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dashboard: {ex.Message}");
            }
        }

        public void Dispose()
        {
            FactoryState.OnChange -= OnFactoryChanged;
        }
    }
}
