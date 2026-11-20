namespace IBusinessLogic;

public interface IInvitationNotificationPublisher
{
    Task PublishAsync(
        InvitationEmailMessage message,
        CancellationToken cancellationToken = default);
}

public record InvitationEmailMessage
{
    public required Guid InvitationId { get; init; }
    public required string UserEmail { get; init; }
    public required Guid CompanyId { get; init; }
    public required string CompanyName { get; init; }
    public required string AreaName { get; init; }
    public required string PlainToken { get; init; }
}
