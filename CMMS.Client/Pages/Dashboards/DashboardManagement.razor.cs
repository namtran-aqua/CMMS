using AntDesign;
using CMMS.Shared.Dtos.DashBoards;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using CMMS.Client.Services;
using System;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.Dashboards
{
    public partial class DashboardManagement : IDisposable
    {
        [Inject]
        public HttpClient HttpClient { get; set; }

        [Inject]
        public FactoryStateService FactoryState { get; set; }

        public List<DashBoarDto> DashBoardData { get; set; } = new();
        private bool isLoading = true;

        private List<DashBoarDto> FilteredDashBoardData
        {
            get
            {
                if (DashBoardData == null) return new();
                if (FactoryState.SelectedFacId.HasValue)
                {
                    return DashBoardData.Where(d => d.FACID == FactoryState.SelectedFacId.Value).ToList();
                }
                return DashBoardData;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            FactoryState.OnChange += OnFactoryChanged;
            await LoadData();
        }

        private async void OnFactoryChanged()
        {
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
                isLoading = true;
                var result = await HttpClient.GetFromJsonAsync<List<DashBoarDto>>("api/DashBoard/dashboard");
                if (result != null)
                {
                    DashBoardData = result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}