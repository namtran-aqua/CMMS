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
                var deptId = FactoryState.SelectedDeptId;
                var url = "api/SparePartDashboard/advanced-dashboard";
                if (facId.HasValue) url += $"?factoryId={facId.Value}";
                if (deptId.HasValue) url += (facId.HasValue ? "&" : "?") + $"departmentId={deptId.Value}";

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

            // 1. Coded Ratio Chart (Pie)
            var codedLabels = new[] { "Coded", "Non-Coded" };
            var codedData = new[] { (double)DashboardData.CodedRatio.CodedCount, (double)DashboardData.CodedRatio.NonCodedCount };
            if (codedData.Sum() > 0)
            {
                await JS.InvokeVoidAsync("renderSpPieChart", "codedRatioChart", codedLabels, codedData, new[] { "#3b82f6", "#10b981" });
            }

            // 2. Stock Status Chart (Doughnut)
            var stockLabels = new[] { "Healthy", "Low"};
            var stockData = new[] { (double)DashboardData.StockStatus.HealthyStock, (double)DashboardData.StockStatus.LowStock };
            if (stockData.Sum() > 0)
            {
                await JS.InvokeVoidAsync("renderSpDoughnutChart", "stockStatusChart", stockLabels, stockData, new[] { "#10b981", "#f59e0b"});
            }

            // 3. Movement Aging Chart (Bar)
            if (DashboardData.MovementAging != null && DashboardData.MovementAging.Any())
            {
                var agingLabels = DashboardData.MovementAging.Select(x => x.Range).ToArray();
                var agingData = DashboardData.MovementAging.Select(x => (double)x.Quantity).ToArray();
                await JS.InvokeVoidAsync("renderSpBarChart", "movementAgingChart", agingLabels, agingData, "Inventory Qty", "#8b5cf6");
            }

            // 4. Low Stock by Location Chart (Bar)
            if (DashboardData.LowStockByLocation != null && DashboardData.LowStockByLocation.Any())
            {
                var locLabels = DashboardData.LowStockByLocation.Select(x => x.Location).ToArray();
                var locData = DashboardData.LowStockByLocation.Select(x => (double)x.Count).ToArray();
                await JS.InvokeVoidAsync("renderSpBarChart", "lowStockByLocationChart", locLabels, locData, "Low Stock SKUs", "#f59e0b");
            }

            // 5. In/Out Trends Chart (Line)
            if (DashboardData.InOutTrends != null && DashboardData.InOutTrends.Any())
            {
                var labels = DashboardData.InOutTrends.Select(x => x.Month).ToArray();
                var imports = DashboardData.InOutTrends.Select(x => (double)x.ImportQuantity).ToArray();
                var exports = DashboardData.InOutTrends.Select(x => (double)x.ExportQuantity).ToArray();
                
                await JS.InvokeVoidAsync("renderSpLineChart", "inOutTrendsChart", labels, imports, exports, "Inbound Orders", "Outbound Orders", "#10b981", "#ef4444");
            }
        }

        public void NavigateToAlerts(string alertLevel)
        {
            Navigation.NavigateTo($"{Navigation.BaseUri}spare-parts/inventory?alert={alertLevel}");
        }

        public async Task ExportToExcel()
        {
            var facId = FactoryState.SelectedFacId;
            var deptId = FactoryState.SelectedDeptId;
            var url = "api/SparePartDashboard/export-excel";
            if (facId.HasValue) url += $"?factoryId={facId.Value}";
            if (deptId.HasValue) url += (facId.HasValue ? "&" : "?") + $"departmentId={deptId.Value}";

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
