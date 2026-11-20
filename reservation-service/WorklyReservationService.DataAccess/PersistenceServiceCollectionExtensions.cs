using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorklyReservationService.DataAccess;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddWorklyReservationPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = ConnectionStringResolver.Resolve(configuration);
        return services.AddMySqlPersistence(connectionString);
    }

    public static IServiceCollection AddWorklyReservationPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddMySqlPersistence(connectionString);
    }
}
