namespace WorklyReservationService.WebApi.Audit;

public interface IAuditEventPublisher
{
    Task PublishAsync(AuditEventMessage message, CancellationToken cancellationToken = default);
}
