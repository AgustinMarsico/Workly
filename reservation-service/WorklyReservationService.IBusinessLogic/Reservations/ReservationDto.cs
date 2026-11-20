namespace WorklyReservationService.IBusinessLogic.Reservations;

public sealed record ReservationDto(
    Guid Id,
    Guid RoomId,
    Guid UserId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool IsCancelled,
    DateTimeOffset? CancelledAtUtc,
    Guid? CancelledByUserId,
    string? CancelledByUserName);
