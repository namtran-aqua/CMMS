using AntDesign;
using CMMS.Shared.Dtos.Barcode;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
        private HashSet<QRCodeItemDto> SelectedItems = new();
        
        private int pageIndex = 1;
        private int pageSize = 10;

        private string SelectedType = "All";
        private string SelectedStatus = "All";
        private string SearchKeyword = "";

        private bool IsLoading = false;
        private bool IsGenerating = false;
        private bool IsExporting = false;
        private bool isSearchPanelCollapsed = false;

        private bool isQrModalVisible = false;
        private QRCodeItemDto? currentItemForQr = null;

        private void ShowQrModal(QRCodeItemDto item)
        {
            currentItemForQr = item;
            isQrModalVisible = true;
        }

        private void CloseQrModal()
        {
            isQrModalVisible = false;
            currentItemForQr = null;
        }

        private async Task PrintSingleBarcode()
        {
            if (currentItemForQr == null) return;
            try
            {
                var request = new ExportPdfRequestDto
                {
                    Items = new List<QRCodeItemDto> { currentItemForQr }
                };

                var response = await Http.PostAsJsonAsync("api/qr/export-pdf", request);
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var base64 = Convert.ToBase64String(fileBytes);
                    var fName = currentItemForQr.BarcodeId + ".pdf";
                    await JSRuntime.InvokeVoidAsync("CMMSJsFunctions.saveAsFile", fName, base64);
                    MessageService.Success("Downloaded PDF for barcode.");
                }
                else
                {
                    MessageService.Error("Failed to export PDF.");
                }
            }
            catch (Exception ex)
            {
                MessageService.Error($"Error: {ex.Message}");
            }
        }

        private bool IsAllSelected => Items.Any() && SelectedItems.Count == Items.Count;

        private void ToggleAll(ChangeEventArgs e)
        {
            bool isChecked = (bool)(e.Value ?? false);
            if (isChecked)
            {
                SelectedItems = new HashSet<QRCodeItemDto>(Items);
            }
            else
            {
                SelectedItems.Clear();
            }
        }

        private void ToggleRow(ChangeEventArgs e, QRCodeItemDto item)
        {
            bool isChecked = (bool)(e.Value ?? false);
            if (isChecked)
            {
                SelectedItems.Add(item);
            }
            else
            {
                SelectedItems.Remove(item);
            }
        }

        private void ToggleRowSelection(QRCodeItemDto item)
        {
            if (SelectedItems.Contains(item))
            {
                SelectedItems.Remove(item);
            }
            else
            {
                SelectedItems.Add(item);
            }
        }

        private void OnPageChange(PaginationEventArgs args)
        {
            pageIndex = args.Page;
            pageSize = args.PageSize;
        }

        private bool HasSelectedNotGenerated => SelectedItems.Any(x => string.IsNullOrEmpty(x.BarcodeId));
        private bool HasSelectedGenerated => SelectedItems.Any(x => !string.IsNullOrEmpty(x.BarcodeId));

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task ResetFilter()
        {
            SelectedType = "All";
            SelectedStatus = "All";
            SearchKeyword = "";
            await LoadData();
        }

        private async Task HandleKeyUp(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await LoadData();
            }
        }

        private async Task LoadData()
        {
            IsLoading = true;
            try
            {
                var url = $"api/qr/items?type={SelectedType}&status={SelectedStatus}&search={Uri.EscapeDataString(SearchKeyword)}";
                Items = await Http.GetFromJsonAsync<List<QRCodeItemDto>>(url) ?? new List<QRCodeItemDto>();
                SelectedItems.Clear();
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
                    string fName = itemsToExport.Count == 1 ? $"{itemsToExport[0].BarcodeId}.pdf" : $"{itemsToExport[0].BarcodeId}_and_{itemsToExport.Count - 1}_others.pdf";
                    await JSRuntime.InvokeVoidAsync("downloadFileFromStream", fName, streamRef);
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


