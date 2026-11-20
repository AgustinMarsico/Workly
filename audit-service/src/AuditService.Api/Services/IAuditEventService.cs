using AuditService.Api.Contracts;

namespace AuditService.Api.Services;

public interface IAuditEventService
{
    Task ValidateAndSaveAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
