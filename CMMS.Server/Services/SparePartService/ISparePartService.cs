using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;

namespace CMMS.Server.Services.SparePartService
{
    public interface ISparePartService
    {
        Task<List<SparePartDto>> GetAllAsync();
        Task<List<SparePartCategoryDto>> GetCategoriesAsync();
        Task<List<SparePartSupplierDto>> GetSuppliersAsync();
        Task<SparePartDto> CreateAsync(SparePartDto dto, UserDto currentUser);
        Task<bool> UpdateAsync(SparePartDto dto, UserDto currentUser);
        Task<bool> DeleteAsync(int spid, UserDto currentUser);
        Task<bool> AdjustStockAsync(AdjustStockRequestDto request, UserDto currentUser);
        Task<string> ExportForMaintenanceAsync(MaintenanceExportRequestDto request, UserDto currentUser);
        Task<List<SparePartTransactionDto>> GetTransactionHistoryAsync();
        Task<SparePartCategoryDto> CreateCategory(SparePartCategoryDto dto, UserDto currentUser);
        Task<bool> DeleteCategory(int categoryid, UserDto currentUser);
        Task<SparePartSupplierDto> CreateSupplier(SparePartSupplierDto dto, UserDto currentUser);
        Task<bool> DeleteSupplier(int supplierid, UserDto currentUser);
    }
}
