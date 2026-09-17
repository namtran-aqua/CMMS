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
                foreach (var label in labels)
                {
                    container.Page(page =>
                    {
                        page.Size(100, 100, Unit.Millimetre);
                        page.Margin(5, Unit.Millimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontFamily("Arial"));

                        page.Content().AlignCenter().AlignMiddle().Column(column =>
                        {
                            var qrImage = GenerateQrCode(label.BarcodeId);

                            column.Item().AlignCenter().Width(65, Unit.Millimetre).Height(65, Unit.Millimetre).Image(qrImage).FitArea();
                            
                            column.Item().AlignCenter().PaddingTop(3, Unit.Millimetre).Text(label.BarcodeId).Bold().FontSize(22);
                            column.Item().AlignCenter().Text(label.EntityName).FontSize(18);
                            
                            if (!string.IsNullOrEmpty(label.AdditionalInfo))
                            {
                                column.Item().AlignCenter().Text(label.AdditionalInfo).FontSize(14).FontColor(Colors.Grey.Darken2);
                            }
                        });
                    });
                }
            });

            return document.GeneratePdf();
        }
    }
}



