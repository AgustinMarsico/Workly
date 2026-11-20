namespace workly_api.Audit;

public interface IAuditEventPublisher
{
    Task PublishAsync(AuditEventMessage message, CancellationToken cancellationToken = default);
}
