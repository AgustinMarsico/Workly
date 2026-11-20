namespace WorklyReservationService.IBusinessLogic.Reservations;

public sealed record UpdateReservationRequest(
    Guid RoomId,
    DateTimeOffset Start,
    DateTimeOffset End);
