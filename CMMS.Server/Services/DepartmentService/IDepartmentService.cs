using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;
namespace CMMS.Server.Services.DepartmentService
{
    public interface IDepartmentService
    {
        Task<List<DepartmentDto>> GetDepartmentsAsync();
        Task<List<DepartmentDto>> GetDepartmentsByFactoryAsync(int factoryId);
        Task<DepartmentDto> GetDepartmentAsync(int id);
        Task<ApiResponse> CreateDepartmentAsync(DepartmentDto department);
        Task<ApiResponse> UpdateDepartmentAsync(DepartmentDto department);
        Task<ApiResponse> DeleteDepartmentAsync(int id);
    }
}
