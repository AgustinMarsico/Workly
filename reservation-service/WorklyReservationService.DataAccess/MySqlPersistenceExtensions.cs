using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WorklyReservationService.DataAccess;

internal static class MySqlPersistenceExtensions
{
    public static IServiceCollection AddMySqlPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        services.AddDbContext<WorklyReservationServiceDbContext>(options =>
        {
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
            options.UseMySql(
                connectionString,
                serverVersion,
                o => o.EnableRetryOnFailure());
        });

        return services;
    }
}
