using Domain;

namespace workly_api.Reservations;

public interface IReservationServiceClient
{
    Task<IReadOnlyList<TopRoomDto>> GetTopRoomsAsync(
        ReservationServiceAuthContext authentication,
        CancellationToken cancellationToken = default);
}
