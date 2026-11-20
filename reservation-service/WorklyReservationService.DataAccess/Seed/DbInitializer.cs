using Microsoft.Extensions.DependencyInjection;
using WorklyReservationService.Domain;

namespace WorklyReservationService.DataAccess.Seed;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorklyReservationServiceDbContext>();

        var companyId = Guid.Parse("447A142D-A99A-4F41-9B08-36AD2EC30B2B");
        var memberUserId = Guid.Parse("496E8B4D-9328-451A-BDC6-CB03C33821CF");
        var operationsUserId = Guid.Parse("F8465060-B637-4932-BC9C-4C4C31E91B8E");

        var roomSalaId = Guid.Parse("8CFC5B35-9C91-4552-9D20-CA22E656B6C6");
        var roomEscritorioId = Guid.Parse("0B47D52E-2019-4B89-BE73-5BB4882FDDE5");
        var roomAuditorioId = Guid.Parse("C1F5A1C3-9919-405D-98E5-761CFA09175D");

        DateTime todayUtc = DateTime.UtcNow.Date;

        var reservations = new[]
        {
            new Reservation
            {
                Id = Guid.Parse("90151A53-6238-4457-A6E6-19D55F8F0D01"),
                CompanyId = companyId,
                RoomId = roomSalaId,
                UserId = memberUserId,
                StartUtc = todayUtc.AddDays(-10).AddHours(10),
                EndUtc = todayUtc.AddDays(-10).AddHours(11),
                CreatedAtUtc = todayUtc.AddDays(-12).AddHours(15)
            },
            new Reservation
            {
                Id = Guid.Parse("8B4A9AD6-974E-4AE0-9658-F6E8B0D4D5A2"),
                CompanyId = companyId,
                RoomId = roomAuditorioId,
                UserId = operationsUserId,
                StartUtc = todayUtc.AddDays(-5).AddHours(9),
                EndUtc = todayUtc.AddDays(-5).AddHours(11),
                CreatedAtUtc = todayUtc.AddDays(-7).AddHours(14),
                CancelledAtUtc = todayUtc.AddDays(-4).AddHours(12),
                CancelledByUserId = operationsUserId
            },
            new Reservation
            {
                Id = Guid.Parse("6B605EB5-47B9-4230-AAD0-8CC3EC171111"),
                CompanyId = companyId,
                RoomId = roomSalaId,
                UserId = memberUserId,
                StartUtc = todayUtc.AddDays(1).AddHours(10),
                EndUtc = todayUtc.AddDays(1).AddHours(11),
                CreatedAtUtc = todayUtc.AddDays(-1).AddHours(10)
            },
            new Reservation
            {
                Id = Guid.Parse("64075770-53AF-4226-938E-9846E9212222"),
                CompanyId = companyId,
                RoomId = roomEscritorioId,
                UserId = memberUserId,
                StartUtc = todayUtc.AddDays(2).AddHours(13),
                EndUtc = todayUtc.AddDays(2).AddHours(15),
                CreatedAtUtc = todayUtc.AddDays(-2).AddHours(16)
            },
            new Reservation
            {
                Id = Guid.Parse("762B6194-F206-4C35-8F7C-39C8D7533333"),
                CompanyId = companyId,
                RoomId = roomAuditorioId,
                UserId = operationsUserId,
                StartUtc = todayUtc.AddDays(7).AddHours(17),
                EndUtc = todayUtc.AddDays(7).AddHours(19),
                CreatedAtUtc = todayUtc.AddDays(-1).AddHours(13)
            }
        };

        foreach (var reservation in reservations)
        {
            if (!db.Reservations.Any(existing => existing.Id == reservation.Id))
            {
                db.Reservations.Add(reservation);
            }
        }

        await db.SaveChangesAsync();
    }
}
