using workly_api.DTOs.Company;

namespace workly_api.Auth;

public interface IIdentityAreaUsersClient
{
    Task<IReadOnlyList<AreaUserResponse>> GetUsersByAreaAsync(
        Guid areaId,
        HttpRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AreaUserResponse>> GetUsersByAreaAsync(
        Guid companyId,
        Guid areaId,
        CancellationToken cancellationToken = default);
}
