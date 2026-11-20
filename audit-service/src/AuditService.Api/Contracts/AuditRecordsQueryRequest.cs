namespace AuditService.Api.Contracts;

public sealed record AuditRecordsQueryRequest
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public Guid? UserId { get; init; }
    public string? ActionType { get; init; }
    public Guid? CompanyId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
