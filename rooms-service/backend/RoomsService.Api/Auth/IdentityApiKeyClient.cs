using System.Net;

namespace workly_api.Auth;

public sealed class IdentityApiKeyClient(HttpClient http, IConfiguration configuration) : IIdentityApiKeyClient
{
    private readonly string validationPath =
        configuration["IdentityService:ApiKeyValidationPath"] ?? "/api/api-keys/validate";

    public async Task<ApiKeyValidationResult> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ApiKeyValidationResult(false);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, validationPath);
        request.Headers.TryAddWithoutValidation("API_KEY", apiKey);

        using HttpResponseMessage response = await http
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return new ApiKeyValidationResult(false);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ApiKeyValidationResult(false);
        }

        return await response.Content
            .ReadFromJsonAsync<ApiKeyValidationResult>(cancellationToken)
            .ConfigureAwait(false)
            ?? new ApiKeyValidationResult(false);
    }
}
