using Microsoft.AspNetCore.Components;

namespace CMMS.Client.Pages.MasterData
{
    public partial class MasterData : ComponentBase
    {
        [Parameter]
        public string SubPath { get; set; } = "";

        private string selectedTab = "Catalog";

        protected override void OnParametersSet()
        {
            selectedTab = SubPath.ToLower() switch
            {
                "spare-part-catalog" => "Catalog",
                "category" => "Category",
                "supplier" => "Supplier",
                "vendor" => "Vendor",
                "location" => "Location",
                "department" => "Department",
                "factory" => "Factory",
                "user" => "User",
                _ => "Catalog"
            };
        }
    }
}
