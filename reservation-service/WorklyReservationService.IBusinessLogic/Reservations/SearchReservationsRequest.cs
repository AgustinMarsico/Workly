namespace WorklyReservationService.IBusinessLogic.Reservations;

public sealed record SearchReservationsRequest(
    Guid? RoomId,
    string? RoomType,
    Guid? UserId,
    Guid? AreaId,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string Status,
    int Page,
    int PageSize);
