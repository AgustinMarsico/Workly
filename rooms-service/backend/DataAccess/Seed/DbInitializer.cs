using DataAccess;
using Domain;
using Domain.RoomTypes;
using Microsoft.Extensions.DependencyInjection;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorklyDbContext>();

        Guid companyId = Guid.Parse("447A142D-A99A-4F41-9B08-36AD2EC30B2B");

        var rooms = new[]
        {
            new Room
            {
                Id = Guid.Parse("8CFC5B35-9C91-4552-9D20-CA22E656B6C6"),
                CompanyId = companyId,
                Name = "Sala Ceibo",
                Type = SalaRoomType.Instance,
                MaxCapacity = 8,
                Equipment = "TV, pizarra y camara para videollamadas"
            },
            new Room
            {
                Id = Guid.Parse("0B47D52E-2019-4B89-BE73-5BB4882FDDE5"),
                CompanyId = companyId,
                Name = "Escritorio Flex 01",
                Type = EscritorioRoomType.Instance,
                MaxCapacity = 1,
                Equipment = "Monitor, docking station y silla ergonomica"
            },
            new Room
            {
                Id = Guid.Parse("C1F5A1C3-9919-405D-98E5-761CFA09175D"),
                CompanyId = companyId,
                Name = "Auditorio Rambla",
                Type = AuditorioRoomType.Instance,
                MaxCapacity = 60,
                Equipment = "Proyector, microfonos y sistema de audio"
            }
        };

        foreach (var room in rooms)
        {
            if (!db.Rooms.Any(existing => existing.Id == room.Id))
            {
                db.Rooms.Add(room);
            }
        }

        await db.SaveChangesAsync();
    }
}
