namespace Domain;

public sealed record TopRoomDto(Guid RoomId, string Name, int ReservationsCount, Guid CompanyId);
