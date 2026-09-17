using System.Threading.Tasks;

namespace CMMS.Server.Services.Barcode
{
    public interface IBarcodeIdService
    {
        Task<string> GenerateEquipmentBarcodeIdAsync(string departmentCode = "MNT");
        Task<string> GenerateSparePartBarcodeIdAsync(string departmentCode = "MNT");
    }
}
