namespace workly_api.DTOs.Company;

public sealed record CompanyResponse(
    Guid Id,
    string Name,
    CompanyAddressResponse Address,
    string Website,
    AreaResponse[] Areas
);

public sealed record AreaResponse(
    Guid Id,
    string Name,
    string Description
);

public sealed record CompanyAddressResponse(
    string Street,
    int Number,
    string City,
    string State,
    string ZipCode,
    string Country
);

public static class CompanyResponseMapper
{
    public static CompanyResponse ToResponse(this Domain.Company company)
    {
        return new CompanyResponse(
            company.Id,
            company.Name,
            new CompanyAddressResponse(
                company.Address.Street,
                company.Address.Number,
                company.Address.City,
                company.Address.State,
                company.Address.ZipCode,
                company.Address.Country),
            company.Website,
            company.Areas.Select(area => new AreaResponse(area.Id, area.Name, area.Description)).ToArray()
        );
    }
}
