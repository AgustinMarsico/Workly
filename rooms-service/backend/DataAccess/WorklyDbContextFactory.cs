using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataAccess;

public class WorklyDbContextFactory : IDesignTimeDbContextFactory<WorklyDbContext>
{
    public WorklyDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "Configure ConnectionStrings__DefaultConnection or DEFAULT_CONNECTION_STRING for design-time operations.");

        var optionsBuilder = new DbContextOptionsBuilder<WorklyDbContext>();
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));

        return new WorklyDbContext(optionsBuilder.Options);
    }
}
