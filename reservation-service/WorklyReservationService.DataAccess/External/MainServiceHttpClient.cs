using System.Net;
using System.Net.Http.Json;
using WorklyReservationService.IDataAccess.External;

namespace WorklyReservationService.DataAccess.External;

public abstract class MainServiceHttpClient
{
    private readonly HttpClient _httpClient;

    protected MainServiceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    protected async Task<T?> GetAsync<T>(
        string path,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        authentication.Apply(request);

        using HttpResponseMessage response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException("Main service rejected the forwarded credentials.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false);
    }

    private void EnsureConfigured()
    {
        if (_httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException("MainService:BaseUrl is not configured.");
        }
    }
}
