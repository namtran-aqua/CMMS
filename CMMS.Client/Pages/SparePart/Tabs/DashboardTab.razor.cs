using CMMS.Client.Services;
using CMMS.Shared.Dtos.SpareParts.Dashboard;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CMMS.Client.Pages.SpareParts.Tabs
{
    public partial class DashboardTab : ComponentBase, IDisposable, IAsyncDisposable
    {
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private FactoryStateService FactoryState { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private DashboardDto DashboardData { get; set; } = new();
        private bool _domReady;

        protected override async Task OnInitializedAsync()
        {
            FactoryState.OnChange += OnFactoryChanged;
            await LoadDashboard();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (DashboardData != null && !_domReady)
            {
                _domReady = true;
                await RenderChartsAsync();
            }
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
                var url = "api/SparePartDashboard/advanced-dashboard";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";

                DashboardData = await Http.GetFromJsonAsync<DashboardDto>(url) ?? new();
                
                if (_domReady && DashboardData != null)
                {
                    await RenderChartsAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dashboard: {ex.Message}");
            }
        }

        private async Task RenderChartsAsync()
        {
            if (DashboardData is null) return;

            try { await JS.InvokeVoidAsync("destroyAllSpCharts"); } catch { }

            // Render Trends Chart (Mock implementation based on existing charts)
            if (DashboardData.Trends != null && DashboardData.Trends.Any())
            {
                var labels = DashboardData.Trends.Select(x => x.Period).Distinct().ToArray();
                var data = DashboardData.Trends.Where(x => x.Type == MovementType.Import).Select(x => (double)x.Quantity).ToArray();
                await JS.InvokeVoidAsync("renderSpBarChart", "topUsedPartsChart", labels, data, "Import Trend", "#10b981");
            }

            // Render Category Value Doughnut Chart
            if (DashboardData.CategoryValues != null && DashboardData.CategoryValues.Any())
            {
                var labels = DashboardData.CategoryValues.Select(x => x.CategoryName).ToArray();
                var data = DashboardData.CategoryValues.Select(x => (double)x.Value).ToArray();
                await JS.InvokeVoidAsync("renderSpDoughnutChart", "categoryValuesChart", labels, data);
            }
        }

        public void NavigateToAlerts(string alertLevel)
        {
            Navigation.NavigateTo($"/spare-parts/inventory?alert={alertLevel}");
        }

        public async Task ExportToExcel()
        {
            var facId = FactoryState.SelectedFacId;
            var url = "api/SparePartDashboard/export-excel";
            if (facId.HasValue) url += $"?factoryId={facId.Value}";

            // We can navigate to the URL to trigger the file download
            await JS.InvokeVoidAsync("window.open", url, "_blank");
        }

        public void Dispose()
        {
            FactoryState.OnChange -= OnFactoryChanged;
        }

        public async ValueTask DisposeAsync()
        {
            try { await JS.InvokeVoidAsync("destroyAllSpCharts"); } catch { }
        }
    }
}
