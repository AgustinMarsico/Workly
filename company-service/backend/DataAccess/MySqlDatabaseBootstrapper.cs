using MySqlConnector;

namespace DataAccess;

/// <summary>
/// Crea la base de datos en el servidor MySQL si aún no existe (las migraciones no lo hacen solas).
/// </summary>
public static class MySqlDatabaseBootstrapper
{
    public static async Task EnsureDatabaseExistsAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        string? databaseName = builder.Database;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException(
                "La cadena de conexión debe incluir el nombre de la base (Database=...).");
        }

        builder.Database = string.Empty;
        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        string safeName = databaseName.Replace("`", "``", StringComparison.Ordinal);
        cmd.CommandText =
            $"CREATE DATABASE IF NOT EXISTS `{safeName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
