namespace WorklyReservationService.WebApi.Audit;

public sealed record AuditEventMessage(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string ActionType,
    Guid? UserId,
    string? UserEmail,
    Guid? CompanyId,
    string? EntityType,
    string? EntityId,
    object? Details,
    object? OldValues,
    object? NewValues,
    string? CorrelationId)
{
    public static AuditEventMessage Create(
        string actionType,
        Guid? userId,
        Guid? companyId,
        string? entityType,
        string? entityId,
        object? details = null,
        object? oldValues = null,
        object? newValues = null,
        string? correlationId = null,
        string? userEmail = null)
    {
        return new AuditEventMessage(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            actionType,
            userId,
            userEmail,
            companyId,
            entityType,
            entityId,
            details,
            oldValues,
            newValues,
            correlationId);
    }
}
