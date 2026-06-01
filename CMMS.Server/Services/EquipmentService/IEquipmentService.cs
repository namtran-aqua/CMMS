using CMMS.Shared.Dtos.Common;
using CMMS.Shared.Dtos.Equipment;

namespace CMMS.Server.Services.EquipmentService
{
    public interface IEquipmentService
    {
        Task<List<EquipmentDto>> GetAllAsync();
        Task<bool> CreatedAsync(EquipmentDto equipment);
        Task<bool> UpdateAsync(EquipmentDto equipment);
        Task<bool> DeleteAsync(int equipmentCode);
        //Task<ApiResponse> RequestScrapAsync(int equipmentCode);
    }
}
