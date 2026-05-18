using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.Dashboards
{
    public partial class DashboardManagement
    {
        [Inject]
        public HttpClient HttpClient { get; set; }

        public List<CMMS.Shared.Dtos.DashBoards.DashBoarDto> DashBoardData { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }
        private async Task LoadData()
        {
            try
            {
                var result = await HttpClient.GetFromJsonAsync<List<DashBoarDto>>("api/DashBoard");
                if (result != null)
                {
                    DashBoardData = result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
        }
    }
}