using WorklyReservationService.IDataAccess.External;

namespace WorklyReservationService.DataAccess.External;

public sealed class MainServiceUserClient : MainServiceHttpClient, IUserClient
{
    public MainServiceUserClient(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public Task<UserResponse?> GetByIdAsync(
        Guid userId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default)
    {
        // The current main service route exists as GET /api/users/{id}, but is Admin-only.
        return GetAsync<UserResponse>($"api/users/{userId}", authentication, cancellationToken);
    }
}
