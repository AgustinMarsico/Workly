using WorklyReservationService.IBusinessLogic.Notifications;

namespace WorklyReservationService.WebApi.Notifications;

public sealed class NoOpReservationNotificationPublisher : IReservationNotificationPublisher
{
    public Task PublishAsync(
        ReservationNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
