namespace WorklyReservationService.IDataAccess;

public sealed record ReservationDashboardItem(
    Guid Id,
    Guid RoomId,
    string? RoomName,
    string? RoomType,
    Guid UserId,
    string? UserEmail,
    Guid? UserAreaId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool IsCancelled,
    DateTimeOffset? CancelledAtUtc,
    Guid? CancelledByUserId,
    string? CancelledByUserName);
