using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.Dashboards
{
    public partial class DashboardManagement
    {
        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }
        private async Task LoadData()
        {
            var a = 1;
        }
    }
}