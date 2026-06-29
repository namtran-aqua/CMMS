using Microsoft.AspNetCore.Components;
using CMMS.Shared.Dtos.Maintenance;
using System.Threading.Tasks;

namespace CMMS.Client.Modals.Maintenances
{
    public partial class MaintenanceDetailModel
    {
        private bool IsVisible { get; set; } = false;
        private MaintenanceDto? Record { get; set; }
        private string EquipmentName { get; set; } = "";

        public void Show(MaintenanceDto record, string equipmentName)
        {
            Record = record;
            EquipmentName = equipmentName;
            IsVisible = true;
            StateHasChanged();
        }

        private void Close()
        {
            IsVisible = false;
        }

        private string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{Math.Round(bytes / (1024.0 * 1024.0), 1)} MB";
            return $"{Math.Round(bytes / 1024.0, 1)} KB";
        }
    }
}
