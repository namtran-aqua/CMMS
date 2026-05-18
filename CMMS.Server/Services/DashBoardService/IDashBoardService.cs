using CMMS.Shared.Dtos.DashBoards;

namespace CMMS.Server.Services.DashBoardService
{
    public interface IDashBoardService
    {
        Task<List<DashBoarDto>> GetDashBoard();

    }
}
