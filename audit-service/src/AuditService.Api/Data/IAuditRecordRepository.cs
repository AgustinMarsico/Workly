using AuditService.Api.Contracts;
using AuditService.Api.Domain;

namespace AuditService.Api.Data;

public interface IAuditRecordRepository
{
    Task AddAsync(AuditRecord record, CancellationToken cancellationToken);
    Task<PagedResult<AuditRecord>> QueryAsync(AuditRecordQuery query, CancellationToken cancellationToken);
}
