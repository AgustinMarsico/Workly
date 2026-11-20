namespace workly_api.Auth;

public interface IIdentityApiKeyClient
{
    Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);
}
