namespace WorklyReservationService.IDataAccess.External;

public interface IUserClient
{
    Task<UserResponse?> GetByIdAsync(
        Guid userId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default);
}
