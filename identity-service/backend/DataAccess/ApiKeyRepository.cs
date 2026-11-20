using Domain;
using IDataAccess;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public sealed class ApiKeyRepository(WorklyDbContext db) : IApiKeyRepository
{
    public async Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        var entry = await db.ApiKeys.AddAsync(apiKey, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApiKey?> GetActiveByHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyHash))
        {
            return null;
        }

        string normalized = keyHash.Trim();

        return await db.ApiKeys
            .AsNoTracking()
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyHash == normalized && k.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> RevokeAsync(Guid apiKeyId, Guid userId, CancellationToken cancellationToken = default)
    {
        ApiKey? tracked = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId && k.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (tracked is null)
        {
            return false;
        }

        if (!tracked.IsActive)
        {
            return true;
        }

        tracked.IsActive = false;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> ExistsActiveByNameForCompanyAsync(
        Guid companyId,
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return false;
        }

        string nameLower = normalizedName.Trim().ToLowerInvariant();

        return await db.ApiKeys
            .AsNoTracking()
            .AnyAsync(
                k => k.IsActive
                    && k.User.CompanyId == companyId
                    && k.Name.ToLower() == nameLower,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<(Guid Id, string Name)>> ListIdAndNameByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.ApiKeys
            .AsNoTracking()
            .Where(k => k.User.CompanyId == companyId && k.IsActive)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new { k.Id, k.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ConvertAll(r => (r.Id, r.Name));
    }
}
