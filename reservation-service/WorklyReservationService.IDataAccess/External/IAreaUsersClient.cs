namespace WorklyReservationService.IDataAccess.External;

public interface IAreaUsersClient
{
    Task<IReadOnlyList<AreaUserResponse>> GetByAreaIdAsync(
        Guid areaId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default);
}
