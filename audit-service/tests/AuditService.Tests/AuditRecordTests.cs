using AuditService.Api.Contracts;
using AuditService.Api.Domain;

namespace AuditService.Tests;

[TestClass]
public sealed class AuditRecordTests
{
    [TestMethod]
    public void FromEvent_ThrowsArgumentException_WhenOccurredAtIsDefault()
    {
        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            default,
            nameof(AuditActionType.ReservationCreated),
            Guid.NewGuid(),
            "admin@workly.test",
            Guid.NewGuid(),
            "Reservation",
            "reservation-1",
            null,
            null,
            null,
            "corr-1");

        ArgumentException ex = Assert.ThrowsException<ArgumentException>(
            () => AuditRecord.FromEvent(auditEvent));

        Assert.AreEqual("occurredAt is required. (Parameter 'auditEvent')", ex.Message);
    }
}
