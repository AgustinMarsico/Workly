namespace WorklyReservationService.IBusinessLogic.Reservations;

public sealed record CreateReservationRequest(
    Guid RoomId,
    DateTimeOffset Start,
    DateTimeOffset End);
