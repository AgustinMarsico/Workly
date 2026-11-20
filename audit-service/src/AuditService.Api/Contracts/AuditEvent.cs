using System.Text.Json;

namespace AuditService.Api.Contracts;

public sealed record AuditEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string ActionType,
    Guid? UserId,
    string? UserEmail,
    Guid? CompanyId,
    string? EntityType,
    string? EntityId,
    JsonElement? Details,
    JsonElement? OldValues,
    JsonElement? NewValues,
    string? CorrelationId);
