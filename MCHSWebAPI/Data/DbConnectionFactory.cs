using Npgsql;
using System.Data;
using MCHSWebAPI.Interfaces;

namespace MCHSWebAPI.Data;

// Фабрика для создания подключений к БД
public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(string connectionString)
    {
        // Добавляем настройки пула соединений для высокой нагрузки
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MinPoolSize = 10,
            MaxPoolSize = 100,
            ConnectionIdleLifetime = 300,
            ConnectionPruningInterval = 10
        };
        _connectionString = builder.ToString();
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
