
using Microsoft.AspNetCore.Components;

namespace CMMS.Client.Shared
{

    public partial class MainLayout
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        private string GetLink(string url)
        {
            var basePath = NavigationManager.BaseUri.TrimEnd('/');
            return $"{basePath}{url}";
        }

    }
}

