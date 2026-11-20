using WorklyReservationService.IDataAccess.External;

namespace WorklyReservationService.DataAccess.External;

public sealed class MainServiceAreaUsersClient : MainServiceHttpClient, IAreaUsersClient
{
    public MainServiceAreaUsersClient(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<AreaUserResponse>> GetByAreaIdAsync(
        Guid areaId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AreaUserResponse>? users = await GetAsync<IReadOnlyList<AreaUserResponse>>(
                $"api/users?areaId={areaId}",
                authentication,
                cancellationToken)
            .ConfigureAwait(false);

        if (users is null)
        {
            throw new InvalidOperationException(
                $"Area users for area {areaId} could not be retrieved from the main service.");
        }

        return users;
    }
}
