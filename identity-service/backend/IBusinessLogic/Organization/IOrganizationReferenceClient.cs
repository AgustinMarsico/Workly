namespace IBusinessLogic.Organization;

public interface IOrganizationReferenceClient
{
    Task<OrganizationAreaReference> GetCompanyAreaAsync(
        Guid companyId,
        Guid areaId,
        CancellationToken cancellationToken = default);
}

public sealed record OrganizationAreaReference(
    Guid CompanyId,
    string CompanyName,
    Guid AreaId,
    string AreaName);
