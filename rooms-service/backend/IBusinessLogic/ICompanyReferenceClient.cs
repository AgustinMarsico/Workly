namespace IBusinessLogic;

public interface ICompanyReferenceClient
{
    Task<bool> ExistsAsync(
        Guid companyId,
        string? authorizationHeader,
        string? apiKey,
        CancellationToken cancellationToken = default);
}
