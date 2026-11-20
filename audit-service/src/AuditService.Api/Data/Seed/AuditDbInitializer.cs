using System.Text.Json;
using AuditService.Api.Contracts;
using AuditService.Api.Domain;

namespace AuditService.Api.Data.Seed;

public static class AuditDbInitializer
{
    public static async Task SeedAsync(AuditDbContext db, CancellationToken cancellationToken = default)
    {
        var companyId = Guid.Parse("447A142D-A99A-4F41-9B08-36AD2EC30B2B");
        var memberUserId = Guid.Parse("496E8B4D-9328-451A-BDC6-CB03C33821CF");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var records = new[]
        {
            CreateRecord(
                Guid.Parse("A115A7B8-2E23-470E-A295-1258B1330001"),
                now.AddDays(-3),
                nameof(AuditActionType.ReservationCreated),
                memberUserId,
                "usuario@saas.com",
                companyId,
                "6B605EB5-47B9-4230-AAD0-8CC3EC171111",
                details: new { Operation = "ReservationCreated", RoomName = "Sala Ceibo" },
                newValues: new
                {
                    RoomId = "8CFC5B35-9C91-4552-9D20-CA22E656B6C6",
                    StartUtc = now.AddDays(1).Date.AddHours(10),
                    EndUtc = now.AddDays(1).Date.AddHours(11)
                }),
            CreateRecord(
                Guid.Parse("A115A7B8-2E23-470E-A295-1258B1330002"),
                now.AddDays(-2),
                nameof(AuditActionType.ReservationUpdated),
                memberUserId,
                "usuario@saas.com",
                companyId,
                "64075770-53AF-4226-938E-9846E9212222",
                details: new { Operation = "ReservationUpdated", RoomName = "Escritorio Flex 01" },
                oldValues: new
                {
                    StartUtc = now.AddDays(2).Date.AddHours(12),
                    EndUtc = now.AddDays(2).Date.AddHours(14)
                },
                newValues: new
                {
                    StartUtc = now.AddDays(2).Date.AddHours(13),
                    EndUtc = now.AddDays(2).Date.AddHours(15)
                }),
            CreateRecord(
                Guid.Parse("A115A7B8-2E23-470E-A295-1258B1330003"),
                now.AddDays(-1),
                nameof(AuditActionType.ReservationCancelled),
                memberUserId,
                "usuario@saas.com",
                companyId,
                "8B4A9AD6-974E-4AE0-9658-F6E8B0D4D5A2",
                details: new { Operation = "ReservationCancelled", RoomName = "Auditorio Rambla" },
                oldValues: new
                {
                    CancelledAtUtc = (DateTimeOffset?)null,
                    CancelledByUserId = (Guid?)null
                },
                newValues: new
                {
                    CancelledAtUtc = now.AddDays(-1),
                    CancelledByUserId = memberUserId
                })
        };

        foreach (var record in records)
        {
            if (!db.AuditRecords.Any(existing => existing.EventId == record.EventId))
            {
                db.AuditRecords.Add(record);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static AuditRecord CreateRecord(
        Guid eventId,
        DateTimeOffset occurredAt,
        string actionType,
        Guid userId,
        string userEmail,
        Guid companyId,
        string entityId,
        object? details = null,
        object? oldValues = null,
        object? newValues = null)
    {
        return AuditRecord.FromEvent(
            new AuditEvent(
                eventId,
                occurredAt,
                actionType,
                userId,
                userEmail,
                companyId,
                "Reservation",
                entityId,
                ToJsonElement(details),
                ToJsonElement(oldValues),
                ToJsonElement(newValues),
                "seed-data"),
            occurredAt);
    }

    private static JsonElement? ToJsonElement(object? value)
    {
        return value is null ? null : JsonSerializer.SerializeToElement(value);
    }
}
