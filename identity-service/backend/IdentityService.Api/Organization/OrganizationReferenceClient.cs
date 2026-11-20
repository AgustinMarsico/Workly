using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using CustomExceptions.OrganizationExceptions;
using IBusinessLogic.Organization;

namespace workly_api.Organization;

public sealed class OrganizationReferenceClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration) : IOrganizationReferenceClient
{
    public async Task<OrganizationAreaReference> GetCompanyAreaAsync(
        Guid companyId,
        Guid areaId,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("CompanyId cannot be empty.", nameof(companyId));
        }

        if (areaId == Guid.Empty)
        {
            throw new ArgumentException("AreaId cannot be empty.", nameof(areaId));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/companies/{companyId}");
        string? serviceKey = configuration["INTERNAL_SERVICE_KEY"];
        if (!string.IsNullOrWhiteSpace(serviceKey))
        {
            request.Headers.TryAddWithoutValidation("X-Service-Key", serviceKey);
        }
        else
        {
            string? authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (AuthenticationHeaderValue.TryParse(authorization, out var authorizationHeader))
            {
                request.Headers.Authorization = authorizationHeader;
            }
        }

        using HttpResponseMessage response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new OrganizationReferenceNotFoundException("Company reference was not found.");
        }

        response.EnsureSuccessStatusCode();

        CompanyResponse? company = await response.Content
            .ReadFromJsonAsync<CompanyResponse>(cancellationToken)
            .ConfigureAwait(false);

        if (company is null || company.Id != companyId)
        {
            throw new OrganizationReferenceNotFoundException("Company reference was not found.");
        }

        if (company.Areas is null || company.Areas.Length == 0)
        {
            return new OrganizationAreaReference(company.Id, company.Name, areaId, "Area");
        }

        AreaResponse? area = company.Areas.FirstOrDefault(a => a.Id == areaId);
        if (area is null)
        {
            throw new OrganizationReferenceNotFoundException("Area reference was not found for the company.");
        }

        return new OrganizationAreaReference(company.Id, company.Name, area.Id, area.Name);
    }

    private sealed record CompanyResponse(
        Guid Id,
        string Name,
        [property: JsonPropertyName("areas")] AreaResponse[]? Areas);

    private sealed record AreaResponse(Guid Id, string Name);
}
