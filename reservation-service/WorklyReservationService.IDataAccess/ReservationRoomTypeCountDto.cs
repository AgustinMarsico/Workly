namespace WorklyReservationService.IDataAccess;

public sealed record ReservationRoomTypeCountDto(
    string RoomTypeKey,
    int Count);
