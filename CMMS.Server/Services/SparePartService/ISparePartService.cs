using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.Dtos.Common;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartService
{
    public interface ISparePartService
    {
        Task<List<SparePartDto>> GetAllAsync(int? factoryId = null);
        Task<SparePartPagedResultDto> GetPagedAsync(int page, int pageSize, string? searchText, int? categoryId, string? stockStatus, string? sortBy, int? factoryId, string? partCode = null, string? partName = null, int? supplierId = null);
        Task<List<SparePartCategoryDto>> GetCategoriesAsync();
        Task<List<SparePartSupplierDto>> GetSuppliersAsync();
        Task<SparePartDto> CreateAsync(SparePartDto dto, UserDto currentUser);
        Task<bool> UpdateAsync(SparePartDto dto, UserDto currentUser);
        Task<bool> DeleteAsync(int spid, UserDto currentUser);
        Task<bool> AdjustStockAsync(AdjustStockRequestDto request, UserDto currentUser);
        Task<string> ExportForMaintenanceAsync(MaintenanceExportRequestDto request, UserDto currentUser);
        Task<List<SparePartTransactionDto>> GetTransactionHistoryAsync(int? factoryId = null);
        Task<PagedResultDto<SparePartTransactionDto>> GetTransactionHistoryPagedAsync(int page, int pageSize, string? searchText, string? typeFilter, int? factoryId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<SparePartCategoryDto> CreateCategory(SparePartCategoryDto dto, UserDto currentUser);
        Task<bool> DeleteCategory(int categoryid, UserDto currentUser);
        Task<SparePartSupplierDto> CreateSupplier(SparePartSupplierDto dto, UserDto currentUser);
        Task<bool> DeleteSupplier(int supplierid, UserDto currentUser);
        Task<ImportResultDto> ImportSparePartsAsync(Stream fileStream, string fileName, UserDto currentUser);

        // Structured Imports and Exports
        Task<ImportOrderDto> CreateImportOrderAsync(ImportOrderDto dto, UserDto currentUser);
        Task<bool> UpdateImportOrderStatusAsync(int importId, string status, UserDto currentUser);
        Task<PagedResultDto<ImportOrderDto>> GetImportOrdersPagedAsync(int page, int pageSize, string? importCode, string? po, int? vendorId, DateTime? fromDate, DateTime? toDate, int? factoryId, Guid? createdBy = null);
        Task<ImportOrderDto?> GetImportOrderDetailAsync(int importId);
        
        Task<ExportOrderDto> CreateExportOrderAsync(ExportOrderDto dto, UserDto currentUser);
        Task<ExportOrderDto> CreateExportOrderInternalAsync(Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction transaction, ExportOrderDto dto, UserDto currentUser);
        Task<bool> UpdateExportOrderStatusAsync(int exportId, string status, UserDto currentUser);
        Task<PagedResultDto<ExportOrderDto>> GetExportOrdersPagedAsync(int page, int pageSize, string? exportCode, int? movementTypeId, DateTime? fromDate, DateTime? toDate, int? factoryId, Guid? createdBy = null);
        Task<ExportOrderDto?> GetExportOrderDetailAsync(int exportId);

        Task<List<MovementTypeDto>> GetMovementTypesAsync(int? factoryId);
        Task<PagedResultDto<SparePartItemDto>> GetCodedSparePartsPagedAsync(int page, int pageSize, string? serialCode, string? partCode, string? partName, string? status, int? factoryId);
        Task<List<SparePartTransactionDto>> GetSparePartMovementHistoryAsync(int spid);
        Task<int?> GetMovementTypeIdByNameAsync(string name);
        
        Task<bool> ClosePeriodAsync(int year, int month, UserDto currentUser);
        Task<PagedResultDto<SparePartMonthlyPeriodDto>> GetMonthlyPeriodsPagedAsync(int page, int pageSize, int? year, int? month, int? factoryId);
        Task<SparePartDashboardDto> GetSparePartDashboardAsync(int? factoryId);
        Task<byte[]> ExportInventoryToExcelAsync(int? factoryId);

        Task<List<ImportOrderDto>> GetImportOrdersAllAsync(int? factoryId = null);
        Task<List<ExportOrderDto>> GetExportOrdersAllAsync(int? factoryId = null);
        Task<List<SparePartItemDto>> GetCodedSparePartsAllAsync(int? factoryId = null);
        Task<List<SparePartMonthlyPeriodDto>> GetMonthlyPeriodsAllAsync(int? factoryId = null);

        // Stock Adjustments
        Task<List<AdjustOrderDto>> GetAdjustOrdersAsync(int? factoryId);
        Task<AdjustOrderDto> GetAdjustOrderByIdAsync(int adjustId);
        Task<AdjustOrderDto> CreateAdjustOrderAsync(CreateAdjustOrderDto dto, UserDto currentUser);
    }
}
