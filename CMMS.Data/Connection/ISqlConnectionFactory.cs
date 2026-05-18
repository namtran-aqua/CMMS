using System.Data;

namespace CMMS.Data.Connection
{
    public interface ISqlConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
