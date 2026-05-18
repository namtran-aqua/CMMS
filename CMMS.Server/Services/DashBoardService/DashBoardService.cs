using CMMS.Data.Connection;
using CMMS.Shared.Dtos.DashBoards;
using Dapper;

namespace CMMS.Server.Services.DashBoardService
{
    public class DashBoardService : IDashBoardService
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public DashBoardService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<DashBoarDto>> GetDashBoard()
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM vw_EquipmentInfo";
            
            var result = await connection.QueryAsync<DashBoarDto>(sql);
            return result.ToList();
        }
    }
}
