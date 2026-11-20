namespace workly_api.DTOs.Company;

public sealed record CreateAreaToCompanyResponse
(
    Guid Id,
    string Name,
    string Description
)
{
    public CreateAreaToCompanyResponse(Domain.Area area) : this(
        area.Id,
        area.Name,
        area.Description
    )
    {
    }
}