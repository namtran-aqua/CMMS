using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.IO;

namespace CMMS.Server.Services.Barcode
{
    public class QRCodeService : IQRCodeService
    {
        static QRCodeService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateQrCode(string data)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }

        public byte[] GeneratePdfLabels(List<LabelInfo> labels)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(10, Unit.Millimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Grid(grid =>
                    {
                        grid.Columns(3); // 3 labels per row
                        grid.Spacing(5, Unit.Millimetre);

                        foreach (var label in labels)
                        {
                            var qrImage = GenerateQrCode(label.BarcodeId);

                            grid.Item().Border(1).BorderColor(Colors.Black).Padding(5).Column(column =>
                            {
                                column.Item().AlignCenter().Width(80).Height(80).Image(qrImage);
                                column.Item().AlignCenter().Text(label.BarcodeId).Bold().FontSize(12);
                                column.Item().AlignCenter().Text(label.EntityName).FontSize(10);
                                if (!string.IsNullOrEmpty(label.AdditionalInfo))
                                {
                                    column.Item().AlignCenter().Text(label.AdditionalInfo).FontSize(9).FontColor(Colors.Grey.Darken2);
                                }
                            });
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
