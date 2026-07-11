using AntDesign;
using CMMS.Shared.Dtos.SpareParts;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace CMMS.Client.Modals.SpareParts
{
    public partial class AdjustStockModal
    {
        [Inject] private HttpClient Http { get; set; }
        [Parameter] public EventCallback OnSave { get; set; }

        private bool IsModalVisible = false;
        private SparePartDto? Part;
        private string Type = "IN";
        private int Qty = 1;
        private string? RefCode;
        private string? Note;

        private int NewStock => Type == "IN" ? (Part?.Inventory ?? 0) + Qty : (Part?.Inventory ?? 0) - Qty;
        private bool IsOverdraw => Type == "OUT" && Qty > (Part?.Inventory ?? 0);
        private bool CanSubmit => Part != null && Qty > 0 && !IsOverdraw;

        public async Task ShowModal(SparePartDto part)
        {
            Part = part;
            Type = "IN";
            Qty = 1;
            RefCode = null;
            Note = null;
            IsModalVisible = true;
            await InvokeAsync(StateHasChanged);
        }

        private void SetType(string type) => Type = type;

        private Task Close()
        {
            IsModalVisible = false;
            return Task.CompletedTask;
        }

        private async Task SaveAsync()
        {
            if (!CanSubmit || Part == null) return;

            var request = new AdjustStockRequestDto
            {
                SPID = Part.SPID,
                Type = Type,
                Qty = Qty,
                RefCode = RefCode,
                Note = Note
            };

            try
            {
                var response = await Http.PostAsJsonAsync("api/SparePart/adjust-stock", request);
                if (response.IsSuccessStatusCode)
                {
                    Message.Success(Type == "IN" ? "Stock in completed" : "Stock out completed");
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