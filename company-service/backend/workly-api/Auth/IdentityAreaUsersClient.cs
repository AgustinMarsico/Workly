using System.Net;
using workly_api.DTOs.Company;

namespace workly_api.Auth;

public sealed class IdentityAreaUsersClient(HttpClient http, IConfiguration configuration)
    : IIdentityAreaUsersClient
{
    public async Task<IReadOnlyList<AreaUserResponse>> GetUsersByAreaAsync(
        Guid areaId,
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        string path = configuration["IdentityService:UsersByAreaPath"] ?? "/api/users";
        string requestUri = $"{path}?areaId={areaId}";

        using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
        ForwardCredentials(request, message);

        using HttpResponseMessage response = await http
            .SendAsync(message, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<AreaUserResponse>>(cancellationToken)
            .ConfigureAwait(false) ?? [];
    }

    public async Task<IReadOnlyList<AreaUserResponse>> GetUsersByAreaAsync(
        Guid companyId,
        Guid areaId,
        CancellationToken cancellationToken = default)
    {
        string path = configuration["IdentityService:UsersByAreaPath"] ?? "/api/users";
        string requestUri = $"{path}?companyId={companyId}&areaId={areaId}";

        using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using HttpResponseMessage response = await http
            .SendAsync(message, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<AreaUserResponse>>(cancellationToken)
            .ConfigureAwait(false) ?? [];
    }

    private static void ForwardCredentials(HttpRequest incoming, HttpRequestMessage outgoing)
    {
        string authorization = incoming.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            outgoing.Headers.TryAddWithoutValidation("Authorization", authorization);
            return;
        }

        string apiKey = incoming.Headers["API_KEY"].ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = incoming.Headers["X-API-KEY"].ToString();
        }

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            outgoing.Headers.TryAddWithoutValidation("API_KEY", apiKey.Trim());
        }
    }
}
