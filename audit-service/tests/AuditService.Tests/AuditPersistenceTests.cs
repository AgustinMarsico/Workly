using System.Text.Json;
using AuditService.Api.Contracts;
using AuditService.Api.Data;
using AuditService.Api.Domain;
using AuditService.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Tests;

[TestClass]
public sealed class AuditPersistenceTests
{
    [TestMethod]
    public async Task ValidateAndSaveAsync_PersistsImmutableAuditRecord()
    {
        await using AuditDbContext db = CreateDbContext();
        var repository = new EfAuditRecordRepository(db);
        var service = new AuditEventService(repository);
        Guid eventId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        await service.ValidateAndSaveAsync(new AuditEvent(
            eventId,
            DateTimeOffset.UtcNow,
            nameof(AuditActionType.ReservationCreated),
            userId,
            "admin@workly.test",
            companyId,
            "Reservation",
            "reservation-1",
            JsonDocument.Parse("""{"roomId":"room-1"}""").RootElement,
            null,
            JsonDocument.Parse("""{"status":"Active"}""").RootElement,
            "corr-1"), CancellationToken.None);

        AuditRecord record = await db.AuditRecords.SingleAsync();
        Assert.AreEqual(eventId, record.EventId);
        Assert.AreEqual(nameof(AuditActionType.ReservationCreated), record.ActionType);
        Assert.AreEqual(userId, record.UserId);
        Assert.AreEqual(companyId, record.CompanyId);
        Assert.AreEqual("""{"roomId":"room-1"}""", record.DetailsJson);

        db.AuditRecords.Remove(record);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [TestMethod]
    public async Task QueryAsync_FiltersByDateUserCompanyAndActionType()
    {
        await using AuditDbContext db = CreateDbContext();
        var repository = new EfAuditRecordRepository(db);
        Guid targetUser = Guid.NewGuid();
        Guid otherUser = Guid.NewGuid();
        Guid targetCompany = Guid.NewGuid();
        Guid otherCompany = Guid.NewGuid();
        DateTimeOffset baseTime = new(2026, 6, 14, 12, 0, 0, TimeSpan.Zero);

        db.AuditRecords.Add(AuditRecord.FromEvent(new AuditEvent(
            Guid.NewGuid(),
            baseTime,
            nameof(AuditActionType.ApiKeyCreated),
            targetUser,
            "admin@workly.test",
            targetCompany,
            "ApiKey",
            "key-1",
            null,
            null,
            null,
            null), baseTime));

        db.AuditRecords.Add(AuditRecord.FromEvent(new AuditEvent(
            Guid.NewGuid(),
            baseTime.AddDays(-3),
            nameof(AuditActionType.ApiKeyCreated),
            targetUser,
            "admin@workly.test",
            targetCompany,
            "ApiKey",
            "key-2",
            null,
            null,
            null,
            null), baseTime));

        db.AuditRecords.Add(AuditRecord.FromEvent(new AuditEvent(
            Guid.NewGuid(),
            baseTime,
            nameof(AuditActionType.ReservationCancelled),
            otherUser,
            "member@workly.test",
            otherCompany,
            "Reservation",
            "reservation-1",
            null,
            null,
            null,
            null), baseTime));

        await db.SaveChangesAsync();

        PagedResult<AuditRecord> result = await repository.QueryAsync(new AuditRecordQuery(
            baseTime.AddDays(-1),
            baseTime.AddDays(1),
            targetUser,
            "apikeycreated",
            targetCompany,
            1,
            10), CancellationToken.None);

        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual(nameof(AuditActionType.ApiKeyCreated), result.Items.Single().ActionType);
        Assert.AreEqual("key-1", result.Items.Single().EntityId);
    }

    private static AuditDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuditDbContext(options);
    }
}
