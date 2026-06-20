using CMMS.Data.Connection;
using Dapper;

namespace CMMS.Server.Services.DailyJobService
{
    public class DailyJobService: IDailyJobService
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public DailyJobService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task UpdateStatusAsync()
        {
            using var conn = _connectionFactory.CreateConnection();

            await conn.ExecuteAsync(@"
                UPDATE Tbl_EquipmentInfo
                SET StsUseID =
                    CASE
                        WHEN DATEADD(DAY, MaintenanceCircleTime, LastMaintenanceDate) < CAST(GETDATE() AS DATE)
                            THEN 3
                        WHEN DATEADD(DAY, MaintenanceCircleTime, LastMaintenanceDate)
                            <= DATEADD(DAY, 7, CAST(GETDATE() AS DATE))
                            THEN 2
                        ELSE 1
                    END");
        }
    }
}
