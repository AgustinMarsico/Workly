using AuditService.Api.Contracts;
using AuditService.Api.Data;
using AuditService.Api.Domain;

namespace AuditService.Api.Services;

public sealed class AuditEventService : IAuditEventService
{
    private readonly IAuditRecordRepository repository;

    public AuditEventService(IAuditRecordRepository repository)
    {
        this.repository = repository;
    }

    public async Task ValidateAndSaveAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        AuditRecord record = AuditRecord.FromEvent(auditEvent);
        await repository.AddAsync(record, cancellationToken);
    }
}
