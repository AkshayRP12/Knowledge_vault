using Microsoft.Data.SqlClient;

namespace KnowledgeVault.API.Data
{
    public class DbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") 
                ?? "Server=.\\SQLEXPRESS;Database=KnowledgeVaultDb;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        public SqlConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
