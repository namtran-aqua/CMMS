using CMMS.Shared.Equipment;
namespace CMMS.Server.Services.DepartmentService
{
    public interface IDepartmentService
    {
        Task<List<DepartmentDto>> GetDepartmentsAsync();
    }
}
