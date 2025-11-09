using Npgsql;
using System.Data;

namespace MCHSProject.ConnectionDB
{
    public class DBConnect
    {
        private readonly string _connectionString;

        public DBConnect(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string is not configured");
        }

        public IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
