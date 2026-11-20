namespace AuditService.Api.Contracts;

public sealed record AuditRecordResponse(
    Guid Id,
    Guid EventId,
    DateTimeOffset OccurredAt,
    string ActionType,
    Guid? UserId,
    string? UserEmail,
    Guid? CompanyId,
    string? EntityType,
    string? EntityId,
    string? DetailsJson,
    string? OldValuesJson,
    string? NewValuesJson,
    string? CorrelationId,
    DateTimeOffset CreatedAt);
