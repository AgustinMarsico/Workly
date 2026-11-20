namespace WorklyReservationService.IDataAccess.External;

public interface IRoomClient
{
    Task<RoomResponse?> GetByIdAsync(
        Guid roomId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, RoomResponse>> GetByIdsAsync(
        IReadOnlyCollection<Guid> roomIds,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken = default);
}
