using Npgsql;
using System.Data;

namespace MCHSWebAPI.Data;

/// <summary>
/// Фабрика подключений к базе данных: умеет создавать новое соединение
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Создаёт новое подключение к базе данных
    /// </summary>
    IDbConnection CreateConnection();
}

/// <summary>
/// Фабрика подключений к базе PostgreSQL.
/// Хранит строку подключения с настройками пула соединений
/// </summary>
public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Готовит строку подключения и включает пул соединений
    /// (от 5 до 50 одновременных соединений)
    /// </summary>
    /// <param name="connectionString">Строка подключения к базе данных</param>
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

    /// <summary>
    /// Создаёт новое подключение к базе PostgreSQL
    /// </summary>
    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
