using System.Net;
using IBusinessLogic;

namespace workly_api.Companies;

public sealed class CompanyReferenceClient : ICompanyReferenceClient
{
    private readonly HttpClient _httpClient;

    public CompanyReferenceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ExistsAsync(
        Guid companyId,
        string? authorizationHeader,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        if (_httpClient.BaseAddress is null)
        {
            return true;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/companies/{companyId}");
        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        }
        else if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.TryAddWithoutValidation("API_KEY", apiKey);
        }

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return true;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}
