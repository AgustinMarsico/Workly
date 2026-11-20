namespace WorklyReservationService.WebApi.Audit;

public sealed class NoOpAuditEventPublisher : IAuditEventPublisher
{
    public Task PublishAsync(AuditEventMessage message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
