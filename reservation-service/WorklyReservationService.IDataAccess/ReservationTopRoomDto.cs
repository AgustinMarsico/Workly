namespace WorklyReservationService.IDataAccess;

public sealed record ReservationTopRoomDto(
    Guid RoomId,
    string Name,
    int ReservationsCount,
    Guid CompanyId);
