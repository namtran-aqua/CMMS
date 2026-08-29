using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CMMS.Server.Services.Barcode
{
    public class LabelInfo
    {
        public string BarcodeId { get; set; }
        public string EntityName { get; set; }
        public string AdditionalInfo { get; set; } // e.g., Code | Serial
    }

    public interface IQRCodeService
    {
        byte[] GenerateQrCode(string data);
        byte[] GeneratePdfLabels(List<LabelInfo> labels);
    }
}
