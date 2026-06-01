using CMMS.Shared.Dtos.Equipment;

namespace CMMS.Server.Services.StatusUsingService
{
    public interface IStatusUsingService
    {
        Task<List<StatusUsingDto>> GetStatusUsingAsync();
    }
}
