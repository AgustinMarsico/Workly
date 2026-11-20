using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace DataAccess;

internal static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var baseConn =
            Environment.GetEnvironmentVariable("DEFAULT_CONNECTION_STRING")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found.");

        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (string.IsNullOrEmpty(password))
        {
            return baseConn;
        }

        var csb = new MySqlConnectionStringBuilder(baseConn)
        {
            Password = password
        };

        return csb.ConnectionString;
    }
}
