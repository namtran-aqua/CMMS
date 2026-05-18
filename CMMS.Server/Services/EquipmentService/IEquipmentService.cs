using CMMS.Shared.EquipmentDto;

namespace CMMS.Server.Services.EquipmentService
{
    public interface IEquipmentService
    {
        Task<List<EquipmentDto>> GetAllAsync();
        Task<bool> CreatedAsync(EquipmentDto equipment);
        Task<bool> UpdateAsync(EquipmentDto equipment);
        Task<bool> DeleteAsync(int equipmentCode);
    }
}
