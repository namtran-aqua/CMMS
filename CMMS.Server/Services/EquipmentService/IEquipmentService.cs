using CMMS.Shared.Dtos.Common;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.User;
using System.IO;

namespace CMMS.Server.Services.EquipmentService
{
    public interface IEquipmentService
    {
        Task<List<EquipmentDto>> GetAllAsync(int? factoryId = null);
        Task<bool> CreatedAsync(EquipmentDto equipment);
        Task<bool> UpdateAsync(EquipmentDto equipment, UserDto currentUser);
        Task<bool> DeleteAsync(int equipmentCode, UserDto currentUser);
        //Task<ApiResponse> RequestScrapAsync(int equipmentCode);
        Task<ImportResultDto> ImportEquipmentsAsync(Stream fileStream, string fileName, UserDto currentUser);
    }
}
