using AntDesign;
using CMMS.Client.Common;
using CMMS.Shared.Dtos.SpareParts;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace CMMS.Client.Modals.SpareParts
{
    public partial class MaintenanceExportModal
    {
        [Inject] private HttpClient Http { get; set; }
        [Parameter] public EventCallback OnSave { get; set; }
        [Parameter] public List<SparePartDto> Parts { get; set; } = new();

        private bool IsModalVisible = false;
        private MaintenanceExportRequestDto Request { get; set; } = new();

        public async Task ShowModal()
        {
            Request = new MaintenanceExportRequestDto
            {
                Lines = new List<MaintenanceExportLineDto>
                {
                    new() { SPID = Parts.FirstOrDefault()?.SPID ?? 0, Qty = 1 }
                }
            };
            IsModalVisible = true;
            await InvokeAsync(StateHasChanged);
        }

        private void AddLine()
        {
            Request.Lines.Add(new MaintenanceExportLineDto { SPID = Parts.FirstOrDefault()?.SPID ?? 0, Qty = 1 });
        }

        private void RemoveLine(MaintenanceExportLineDto line)
        {
            Request.Lines.Remove(line);
        }

        private Task Close()
        {
            IsModalVisible = false;
            return Task.CompletedTask;
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Request.Equipment))
            {
                Message.Error("Please enter equipment / maintenance job.");
                return;
            }
            if (!Request.Lines.Any(l => l.Qty > 0))
            {
                Message.Error("Please add at least one part.");
                return;
            }

            try
            {
                var response = await Http.PostAsJsonAsync("api/SparePart/export-maintenance", Request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    Message.Success($"Exported. Reference: {result?["refCode"]}");
                    IsModalVisible = false;
                    await OnSave.InvokeAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Message.Error(error);
                }
            }
            catch (Exception ex)
            {
                Message.Error($"Error: {ex.Message}");
            }
        }
    }
}