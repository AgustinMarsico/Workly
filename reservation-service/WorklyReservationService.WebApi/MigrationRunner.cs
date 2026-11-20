using Microsoft.EntityFrameworkCore;
using WorklyReservationService.DataAccess;
using WorklyReservationService.DataAccess.Seed;

namespace WorklyReservationService.WebApi;

public static class MigrationRunner
{
    public static async Task Run(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorklyReservationServiceDbContext>();
        await db.Database.MigrateAsync();
        await DbInitializer.SeedAsync(services);
    }
}
