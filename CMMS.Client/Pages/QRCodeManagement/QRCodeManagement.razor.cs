using AntDesign;
using CMMS.Shared.Dtos.Barcode;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CMMS.Client.Pages.QRCodeManagement
{
    public partial class QRCodeManagement : ComponentBase
    {
        private List<QRCodeItemDto> Items = new();
        private IEnumerable<QRCodeItemDto> SelectedItems = new List<QRCodeItemDto>();
        private ITable table;

        private string SelectedType = "All";
        private string SelectedStatus = "NotGenerated";
        private string SearchKeyword = "";

        private bool IsLoading = false;
        private bool IsGenerating = false;
        private bool IsExporting = false;

        private bool HasSelectedNotGenerated => SelectedItems != null && SelectedItems.Any(x => string.IsNullOrEmpty(x.BarcodeId));
        private bool HasSelectedGenerated => SelectedItems != null && SelectedItems.Any(x => !string.IsNullOrEmpty(x.BarcodeId));

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            IsLoading = true;
            try
            {
                var url = $"api/qr/items?type={SelectedType}&status={SelectedStatus}&search={Uri.EscapeDataString(SearchKeyword)}";
                Items = await Http.GetFromJsonAsync<List<QRCodeItemDto>>(url) ?? new List<QRCodeItemDto>();
                SelectedItems = new List<QRCodeItemDto>();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Error loading data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private async Task GenerateBarcodes()
        {
            var itemsToGenerate = SelectedItems.Where(x => string.IsNullOrEmpty(x.BarcodeId)).ToList();
            if (!itemsToGenerate.Any()) return;

            IsGenerating = true;
            try
            {
                var response = await Http.PostAsJsonAsync("api/qr/generate", new GenerateBarcodeRequestDto { Items = itemsToGenerate });
                if (response.IsSuccessStatusCode)
                {
                    MessageService.Success("Barcode IDs generated successfully.");
                    await LoadData();
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageService.Error($"Generation failed: {err}");
                }
            }
            catch (Exception ex)
            {
                MessageService.Error($"Error generating barcodes: {ex.Message}");
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private async Task ExportPdf()
        {
            var itemsToExport = SelectedItems.Where(x => !string.IsNullOrEmpty(x.BarcodeId)).ToList();
            if (!itemsToExport.Any()) return;

            IsExporting = true;
            try
            {
                var response = await Http.PostAsJsonAsync("api/qr/export-pdf", new ExportPdfRequestDto { Items = itemsToExport });
                if (response.IsSuccessStatusCode)
                {
                    var fileStream = await response.Content.ReadAsStreamAsync();
                    using var streamRef = new DotNetStreamReference(stream: fileStream);
                    await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "qr_labels.pdf", streamRef);
                    MessageService.Success("PDF exported successfully.");
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageService.Error($"Export failed: {err}");
                }
            }
            catch (Exception ex)
            {
                MessageService.Error($"Error exporting PDF: {ex.Message}");
            }
            finally
            {
                IsExporting = false;
            }
        }
    }
}
