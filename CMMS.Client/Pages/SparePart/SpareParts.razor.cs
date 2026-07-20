using Microsoft.AspNetCore.Components;

namespace CMMS.Client.Pages.SpareParts
{
    public partial class SpareParts : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; }

        [Parameter] public string? SubPath { get; set; }

        private string selectedTab = "Dashboard";

        protected override void OnParametersSet()
        {
            if (!string.IsNullOrEmpty(SubPath))
            {
                selectedTab = SubPath.ToLower() switch
                {
                    "dashboard" => "Dashboard",
                    "catalog" => "Catalog",
                    "inventory" => "Inventory",
                    "coded-parts" => "CodedParts",
                    "imports" => "Imports",
                    "exports" => "Exports",
                    "adjustments" => "Adjustments",
                    "monthly-closing" => "MonthlyClosing",
                    "history" => "History",
                    _ => "Dashboard"
                };
            }
            else
            {
                selectedTab = "Dashboard";
            }
        }
    }
}