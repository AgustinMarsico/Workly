using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using WorklyReservationService.IBusinessLogic.Notifications;

namespace WorklyReservationService.WebApi.Notifications;

public sealed class SnsReservationNotificationPublisher : IReservationNotificationPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IAmazonSimpleNotificationService _sns;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SnsReservationNotificationPublisher> _logger;

    public SnsReservationNotificationPublisher(
        IAmazonSimpleNotificationService sns,
        IConfiguration configuration,
        ILogger<SnsReservationNotificationPublisher> logger)
    {
        _sns = sns;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishAsync(
        ReservationNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        string? topicArn = _configuration["SNS_RESERVATION_TOPIC_ARN"];
        if (string.IsNullOrWhiteSpace(topicArn))
        {
            return;
        }

        try
        {
            var request = new PublishRequest
            {
                TopicArn = topicArn,
                Message = JsonSerializer.Serialize(message, JsonOptions),
            };

            await _sns.PublishAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish reservation notification for reservation {ReservationId}; business flow continues.",
                message.Reservation.Id);
        }
    }
}
