using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace DataAccess;

public class WorklyDbContextFactory : IDesignTimeDbContextFactory<WorklyDbContext>
{
    public WorklyDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = ConnectionStringResolver.Resolve(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<WorklyDbContext>();

        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));

        return new WorklyDbContext(optionsBuilder.Options);
    }
}
