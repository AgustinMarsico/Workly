namespace WorklyReservationService.IBusinessLogic.Notifications;

public interface IReservationNotificationPublisher
{
    Task PublishAsync(
        ReservationNotificationMessage message,
        CancellationToken cancellationToken = default);
}

public sealed class ReservationNotificationMessage
{
    public required IReadOnlyList<string> Emails { get; init; }

    public bool EmailSubscribed { get; init; } = true;

    public required string Action { get; init; }

    public required DateTime ActionAt { get; init; }

    public required ReservationNotificationReservation Reservation { get; init; }

    public required ReservationNotificationUser User { get; init; }

    public IReadOnlyList<ReservationNotificationChange>? Changes { get; init; }
}

public sealed class ReservationNotificationReservation
{
    public required string Id { get; init; }

    public required ReservationNotificationSpace Space { get; init; }

    public required DateTime StartAt { get; init; }

    public required DateTime EndAt { get; init; }
}

public sealed class ReservationNotificationSpace
{
    public required string Name { get; init; }
}

public sealed class ReservationNotificationUser
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }
}

public sealed class ReservationNotificationChange
{
    public required string Field { get; init; }

    public required string Previous { get; init; }

    public required string Current { get; init; }
}
