using System.Threading.Tasks;

namespace CMMS.Server.Services.Barcode
{
    public interface IBarcodeIdService
    {
        Task EnsureTableExistsAsync();
        Task<string> GenerateEquipmentBarcodeIdAsync(string departmentCode);
        Task<string> GenerateSparePartBarcodeIdAsync(string departmentCode);
    }
}
