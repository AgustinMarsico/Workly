using IBusinessLogic.ApiKeys;

namespace IBusinessLogic;

public interface IApiKeyService
{
    Task<CreatedApiKeyResult> CreateAsync(
        Guid userId,
        Guid companyId,
        string name,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        Guid userId,
        Guid companyId,
        Guid apiKeyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApiKeyListItem>> ListAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<ApiKeyValidationResult> ValidateAsync(
        string apiKeyValue,
        CancellationToken cancellationToken = default);
}
