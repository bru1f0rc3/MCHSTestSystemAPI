using Npgsql;
using System.Data;

namespace MCHSWebAPI.Data;
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MinPoolSize = 5,
            MaxPoolSize = 50
        };
        _connectionString = builder.ToString();
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
