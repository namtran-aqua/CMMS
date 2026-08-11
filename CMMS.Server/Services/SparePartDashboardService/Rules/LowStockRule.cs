using CMMS.Shared.Dtos.SpareParts.Dashboard;
using System.Threading.Tasks;

namespace CMMS.Server.Services.SparePartDashboardService.Rules
{
    public class LowStockRule : IAlertRule
    {
        // In a real application, inject DbContext or IDbConnection here
        // private readonly ApplicationDbContext _dbContext;

        public async Task<int> EvaluateAsync(DashboardFilterDto filter, CMMS.Data.Connection.ISqlConnectionFactory connectionFactory)
        {
            using var connection = connectionFactory.CreateConnection();
            var sql = @"
                SELECT COUNT(1)
                FROM dbo.Tbl_SparePart
                WHERE Inventory > 0 AND Inventory <= MinStock
                  AND (@FactoryId IS NULL OR FACID = @FactoryId)";
                  
            return await Dapper.SqlMapper.QueryFirstOrDefaultAsync<int>(connection, sql, new { FactoryId = filter.FactoryId });
        }
    }
}
