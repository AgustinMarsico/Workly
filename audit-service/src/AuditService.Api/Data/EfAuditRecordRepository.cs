using AuditService.Api.Contracts;
using AuditService.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Api.Data;

public sealed class EfAuditRecordRepository : IAuditRecordRepository
{
    private const int MaxPageSize = 200;
    private readonly AuditDbContext db;

    public EfAuditRecordRepository(AuditDbContext db)
    {
        this.db = db;
    }

    public async Task AddAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        bool alreadyExists = await db.AuditRecords
            .AsNoTracking()
            .AnyAsync(existing => existing.EventId == record.EventId, cancellationToken);

        if (alreadyExists)
        {
            return;
        }

        db.AuditRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditRecord>> QueryAsync(
        AuditRecordQuery query,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        IQueryable<AuditRecord> records = db.AuditRecords.AsNoTracking();

        if (query.From.HasValue)
        {
            records = records.Where(record => record.OccurredAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            records = records.Where(record => record.OccurredAt <= query.To.Value);
        }

        if (query.UserId.HasValue)
        {
            records = records.Where(record => record.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ActionType))
        {
            string normalizedActionType = AuditActionTypes.Normalize(query.ActionType);
            records = records.Where(record => record.ActionType == normalizedActionType);
        }

        if (query.CompanyId.HasValue)
        {
            records = records.Where(record => record.CompanyId == query.CompanyId.Value);
        }

        int totalCount = await records.CountAsync(cancellationToken);
        List<AuditRecord> items = await records
            .OrderByDescending(record => record.OccurredAt)
            .ThenByDescending(record => record.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditRecord>(items, page, pageSize, totalCount);
    }
}
