using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using IBusinessLogic;

namespace workly_api.Notifications;

public sealed class SnsInvitationNotificationPublisher : IInvitationNotificationPublisher
{
    private readonly IAmazonSimpleNotificationService sns;
    private readonly IConfiguration configuration;
    private readonly ILogger<SnsInvitationNotificationPublisher> logger;

    public SnsInvitationNotificationPublisher(
        IAmazonSimpleNotificationService sns,
        IConfiguration configuration,
        ILogger<SnsInvitationNotificationPublisher> logger)
    {
        this.sns = sns;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task PublishAsync(
        InvitationEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        string? topicArn = configuration["SNS_INVITATION_TOPIC_ARN"];
        if (string.IsNullOrWhiteSpace(topicArn))
        {
            return;
        }

        try
        {
            var request = new PublishRequest
            {
                TopicArn = topicArn,
                Message = JsonSerializer.Serialize(message),
            };

            await sns.PublishAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish invitation notification {InvitationId}; business flow continues.",
                message.InvitationId);
        }
    }
}
