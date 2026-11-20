using IBusinessLogic;

namespace workly_api.Notifications;

public sealed class NoOpInvitationNotificationPublisher : IInvitationNotificationPublisher
{
    public Task PublishAsync(
        InvitationEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
