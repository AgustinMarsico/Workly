using System.Text.Json;
using AuditService.Api.Contracts;
using AuditService.Api.Data;
using AuditService.Api.Domain;
using AuditService.Api.Services;
using Moq;

namespace AuditService.Tests;

[TestClass]
public sealed class AuditEventServiceTests
{
    private Mock<IAuditRecordRepository>? _repository;
    private AuditEventService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new Mock<IAuditRecordRepository>();
        _service = new AuditEventService(_repository.Object);
    }

    [TestMethod]
    public async Task ValidateAndSaveAsync_AddsAuditRecord_WhenEventIsValid()
    {
        Guid eventId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();
        DateTimeOffset occurredAt = new(2026, 6, 14, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset beforeSave = DateTimeOffset.UtcNow;
        AuditRecord? captured = null;
        _repository!
            .Setup(r => r.AddAsync(It.IsAny<AuditRecord>(), It.IsAny<CancellationToken>()))
            .Callback<AuditRecord, CancellationToken>((record, _) => captured = record)
            .Returns(Task.CompletedTask);
        var auditEvent = CreateAuditEvent(
            eventId,
            occurredAt,
            nameof(AuditActionType.ReservationCreated),
            userId,
            companyId,
            detailsJson: """{"roomId":"room-1"}""");

        await _service.ValidateAndSaveAsync(auditEvent, CancellationToken.None);

        DateTimeOffset afterSave = DateTimeOffset.UtcNow;
        Assert.IsNotNull(captured);
        Assert.AreNotEqual(Guid.Empty, captured!.Id);
        Assert.AreEqual(eventId, captured.EventId);
        Assert.AreEqual(occurredAt, captured.OccurredAt);
        Assert.AreEqual(nameof(AuditActionType.ReservationCreated), captured.ActionType);
        Assert.AreEqual(userId, captured.UserId);
        Assert.AreEqual(companyId, captured.CompanyId);
        Assert.AreEqual("Reservation", captured.EntityType);
        Assert.AreEqual("reservation-1", captured.EntityId);
        Assert.AreEqual("""{"roomId":"room-1"}""", captured.DetailsJson);
        Assert.IsTrue(captured.CreatedAt >= beforeSave);
        Assert.IsTrue(captured.CreatedAt <= afterSave);
        _repository.Verify(r => r.AddAsync(It.IsAny<AuditRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ValidateAndSaveAsync_AddsAuditRecordWithNullOptionalFields_WhenEventHasNullOrEmptyValues()
    {
        AuditRecord? captured = null;
        _repository!
            .Setup(r => r.AddAsync(It.IsAny<AuditRecord>(), It.IsAny<CancellationToken>()))
            .Callback<AuditRecord, CancellationToken>((record, _) => captured = record)
            .Returns(Task.CompletedTask);
        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 6, 14, 12, 0, 0, TimeSpan.Zero),
            "apikeycreated",
            null,
            "   ",
            null,
            "  ",
            string.Empty,
            null,
            JsonDocument.Parse("null").RootElement,
            null,
            " ");

        await _service.ValidateAndSaveAsync(auditEvent, CancellationToken.None);

        Assert.IsNotNull(captured);
        Assert.AreEqual(nameof(AuditActionType.ApiKeyCreated), captured!.ActionType);
        Assert.IsNull(captured.UserId);
        Assert.IsNull(captured.UserEmail);
        Assert.IsNull(captured.CompanyId);
        Assert.IsNull(captured.EntityType);
        Assert.IsNull(captured.EntityId);
        Assert.IsNull(captured.DetailsJson);
        Assert.IsNull(captured.OldValuesJson);
        Assert.IsNull(captured.NewValuesJson);
        Assert.IsNull(captured.CorrelationId);
        _repository.Verify(r => r.AddAsync(It.IsAny<AuditRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ValidateAndSaveAsync_ThrowsException_WhenRepositoryThrowsException()
    {
        var expected = new InvalidOperationException("repository failed");
        _repository!
            .Setup(r => r.AddAsync(It.IsAny<AuditRecord>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expected);
        var auditEvent = CreateAuditEvent();

        InvalidOperationException ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _service.ValidateAndSaveAsync(auditEvent, CancellationToken.None));

        Assert.AreEqual(expected.Message, ex.Message);
        _repository.Verify(r => r.AddAsync(It.IsAny<AuditRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ValidateAndSaveAsync_ThrowsArgumentExceptionAndDoesNotAddRecord_WhenEventIdIsEmpty()
    {
        var auditEvent = CreateAuditEvent(eventId: Guid.Empty);

        ArgumentException ex = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.ValidateAndSaveAsync(auditEvent, CancellationToken.None));

        Assert.AreEqual("eventId is required. (Parameter 'auditEvent')", ex.Message);
        _repository!.Verify(r => r.AddAsync(It.IsAny<AuditRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AuditEvent CreateAuditEvent(
        Guid? eventId = null,
        DateTimeOffset? occurredAt = null,
        string actionType = nameof(AuditActionType.ReservationCreated),
        Guid? userId = null,
        Guid? companyId = null,
        string detailsJson = """{"source":"test"}""")
    {
        return new AuditEvent(
            eventId ?? Guid.NewGuid(),
            occurredAt ?? new DateTimeOffset(2026, 6, 14, 12, 0, 0, TimeSpan.Zero),
            actionType,
            userId ?? Guid.NewGuid(),
            "admin@workly.test",
            companyId ?? Guid.NewGuid(),
            "Reservation",
            "reservation-1",
            JsonDocument.Parse(detailsJson).RootElement,
            null,
            null,
            "corr-1");
    }
}
