namespace AuditService.Api.Data;

public sealed record AuditRecordQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    Guid? UserId,
    string? ActionType,
    Guid? CompanyId,
    int Page,
    int PageSize);
