using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace workly_api;

public static class MigrationRunner
{
    public static async Task Run(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorklyDbContext>();
        await db.Database.MigrateAsync();
        await DbInitializer.SeedAsync(services);
    }
}