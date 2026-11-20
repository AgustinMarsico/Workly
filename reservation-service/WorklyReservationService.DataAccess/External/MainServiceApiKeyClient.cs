using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using WorklyReservationService.IDataAccess.External;

namespace WorklyReservationService.DataAccess.External;

public sealed class MainServiceApiKeyClient : IApiKeyClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _validationPath;

    public MainServiceApiKeyClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _validationPath = configuration["IdentityService:ApiKeyValidationPath"]
            ?? configuration["MainService:ApiKeyValidationPath"];
    }

    public async Task<ApiKeyValidationResponse?> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (_httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException("IdentityService:BaseUrl is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_validationPath))
        {
            throw new NotSupportedException(
                "The identity service does not expose an API key validation endpoint yet. " +
                "Expose one and configure IdentityService:ApiKeyValidationPath.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, _validationPath);
        request.Headers.TryAddWithoutValidation("API_KEY", apiKey);

        using HttpResponseMessage response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<ApiKeyValidationResponse>(cancellationToken)
            .ConfigureAwait(false);
    }
}
