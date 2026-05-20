using AntDesign;
using CMMS.Shared.Dtos.DashBoards;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.Dashboards
{
    public partial class DashboardManagement
    {
        [Inject]
        public HttpClient HttpClient { get; set; }

        public List<DashBoarDto> DashBoardData { get; set; } = new();
        private bool isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
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