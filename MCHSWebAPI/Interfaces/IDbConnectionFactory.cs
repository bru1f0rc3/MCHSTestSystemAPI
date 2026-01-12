using System.Data;

namespace MCHSWebAPI.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
