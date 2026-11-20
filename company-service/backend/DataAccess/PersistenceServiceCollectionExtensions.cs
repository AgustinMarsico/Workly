using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace DataAccess;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddWorklyPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = ConnectionStringResolver.Resolve(configuration);
        return services.AddMySqlPersistence(connectionString);
    }

    public static IServiceCollection AddWorklyPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddMySqlPersistence(connectionString);
    }
}
