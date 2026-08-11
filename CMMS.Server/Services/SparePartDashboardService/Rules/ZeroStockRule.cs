using CMMS.Shared.Dtos.SpareParts.Dashboard;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartDashboardService.Rules
{
    public class ZeroStockRule : IAlertRule
    {
        public async Task<int> EvaluateAsync(DashboardFilterDto filter, CMMS.Data.Connection.ISqlConnectionFactory connectionFactory)
        {
            using var connection = connectionFactory.CreateConnection();
            var sql = @"
                SELECT COUNT(1)
                FROM dbo.Tbl_SparePart
                WHERE Inventory <= 0 AND MinStock > 0
                  AND (@FactoryId IS NULL OR FACID = @FactoryId)";
                  
            return await Dapper.SqlMapper.QueryFirstOrDefaultAsync<int>(connection, sql, new { FactoryId = filter.FactoryId });
        }
    }
}
