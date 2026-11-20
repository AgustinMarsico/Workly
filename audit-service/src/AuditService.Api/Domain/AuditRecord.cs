using AuditService.Api.Contracts;

namespace AuditService.Api.Domain;

public sealed class AuditRecord
{
    private AuditRecord()
    {
        ActionType = string.Empty;
    }

    private AuditRecord(
        Guid id,
        Guid eventId,
        DateTimeOffset occurredAt,
        string actionType,
        Guid? userId,
        string? userEmail,
        Guid? companyId,
        string? entityType,
        string? entityId,
        string? detailsJson,
        string? oldValuesJson,
        string? newValuesJson,
        string? correlationId,
        DateTimeOffset createdAt)
    {
        Id = id;
        EventId = eventId;
        OccurredAt = occurredAt;
        ActionType = actionType;
        UserId = userId;
        UserEmail = userEmail;
        CompanyId = companyId;
        EntityType = entityType;
        EntityId = entityId;
        DetailsJson = detailsJson;
        OldValuesJson = oldValuesJson;
        NewValuesJson = newValuesJson;
        CorrelationId = correlationId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string ActionType { get; private set; }
    public Guid? UserId { get; private set; }
    public string? UserEmail { get; private set; }
    public Guid? CompanyId { get; private set; }
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string? DetailsJson { get; private set; }
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditRecord FromEvent(AuditEvent auditEvent, DateTimeOffset? createdAt = null)
    {
        if (auditEvent.EventId == Guid.Empty)
        {
            throw new ArgumentException("eventId is required.", nameof(auditEvent));
        }

        if (auditEvent.OccurredAt == default)
        {
            throw new ArgumentException("occurredAt is required.", nameof(auditEvent));
        }

        string actionType = AuditActionTypes.Normalize(auditEvent.ActionType);

        return new AuditRecord(
            Guid.NewGuid(),
            auditEvent.EventId,
            auditEvent.OccurredAt,
            actionType,
            auditEvent.UserId,
            TrimToNull(auditEvent.UserEmail),
            auditEvent.CompanyId,
            TrimToNull(auditEvent.EntityType),
            TrimToNull(auditEvent.EntityId),
            AuditJson.ToRawTextOrNull(auditEvent.Details),
            AuditJson.ToRawTextOrNull(auditEvent.OldValues),
            AuditJson.ToRawTextOrNull(auditEvent.NewValues),
            TrimToNull(auditEvent.CorrelationId),
            createdAt ?? DateTimeOffset.UtcNow);
    }

    private static string? TrimToNull(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
