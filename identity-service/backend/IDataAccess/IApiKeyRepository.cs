using Domain;

namespace IDataAccess;

public interface IApiKeyRepository
{
    Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetActiveByHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid apiKeyId, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveByNameForCompanyAsync(
        Guid companyId,
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(Guid Id, string Name)>> ListIdAndNameByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}
