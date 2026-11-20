using WorklyReservationService.IDataAccess.External;

namespace WorklyReservationService.DataAccess.External;

public sealed class MainServiceRoomClient : MainServiceHttpClient, IRoomClient
{
    public MainServiceRoomClient(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public Task<RoomResponse?> GetByIdAsync(
        Guid roomId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<RoomResponse>($"api/rooms/{roomId}", authentication, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, RoomResponse>> GetByIdsAsync(
        IReadOnlyCollection<Guid> roomIds,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default)
    {
        if (roomIds.Count == 0)
        {
            return new Dictionary<Guid, RoomResponse>();
        }

        HashSet<Guid> requestedIds = roomIds.ToHashSet();
        IReadOnlyList<RoomResponse> rooms = await GetAsync<IReadOnlyList<RoomResponse>>(
                "api/rooms",
                authentication,
                cancellationToken)
            .ConfigureAwait(false) ?? [];

        return rooms
            .Where(room => requestedIds.Contains(room.Id))
            .ToDictionary(room => room.Id);
    }
}
