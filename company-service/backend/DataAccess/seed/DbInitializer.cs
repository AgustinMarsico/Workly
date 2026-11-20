using DataAccess;
using Domain;
using Microsoft.Extensions.DependencyInjection;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorklyDbContext>();

        var company = new Company
        {
            Id = Guid.Parse("447A142D-A99A-4F41-9B08-36AD2EC30B2B"),
            Name = "Empresa Demo",
            Website = "https://example.com",
            Base64Logo = "logo",
            Address = new Address
            {
                Street = "Calle Demo",
                Number = 100,
                City = "Ciudad Demo",
                State = "Estado Demo",
                ZipCode = "00000",
                Country = "Pais Demo"
            }
        };

        if (!db.Companies.Any(existing => existing.Id == company.Id))
        {
            db.Companies.Add(company);
        }

        var areas = new[]
        {
            new Area
            {
                Id = Guid.Parse("5F7F39B8-3B6D-4F7B-AD43-4C651CF62B01"),
                CompanyId = company.Id,
                Name = "Ingenieria",
                Description = "Equipo de desarrollo y arquitectura."
            },
            new Area
            {
                Id = Guid.Parse("F4DE1657-B83E-4EAB-9AE3-AF3A806B2A5C"),
                CompanyId = company.Id,
                Name = "Diseno",
                Description = "Equipo de experiencia de usuario y producto."
            },
            new Area
            {
                Id = Guid.Parse("FE7150D1-D7F1-4792-A54B-783E14DBA938"),
                CompanyId = company.Id,
                Name = "Operaciones",
                Description = "Equipo de coordinacion operativa."
            }
        };

        foreach (var area in areas)
        {
            if (!db.Set<Area>().Any(existing => existing.Id == area.Id))
            {
                db.Set<Area>().Add(area);
            }
        }

        await db.SaveChangesAsync();
    }
}
