using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace WorklyReservationService.WebApi.Audit;

public sealed class SqsAuditEventPublisher : IAuditEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAmazonSQS _sqs;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqsAuditEventPublisher> _logger;

    public SqsAuditEventPublisher(
        IAmazonSQS sqs,
        IConfiguration configuration,
        ILogger<SqsAuditEventPublisher> logger)
    {
        _sqs = sqs;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishAsync(AuditEventMessage message, CancellationToken cancellationToken = default)
    {
        string? queueUrl = _configuration["SQS_AUDIT_QUEUE_URL"];
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            return;
        }

        try
        {
            var request = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = JsonSerializer.Serialize(message, JsonOptions),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>()
            };

            if (!string.IsNullOrWhiteSpace(message.CorrelationId))
            {
                request.MessageAttributes["X-Correlation-ID"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = message.CorrelationId
                };
            }

            if (queueUrl.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase))
            {
                request.MessageGroupId = message.CompanyId?.ToString() ?? "workly-audit";
                request.MessageDeduplicationId = message.EventId.ToString();
            }

            SendMessageResponse response = await _sqs.SendMessageAsync(request, cancellationToken);
            _logger.LogInformation(
                "SQS audit event published: Queue={QueueUrl} EventId={EventId} MessageId={MessageId} Status={Status} CorrelationId={CorrelationId}",
                queueUrl,
                message.EventId,
                response.MessageId,
                "processed",
                message.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SQS audit event publish failed: Queue={QueueUrl} EventId={EventId} Status={Status} CorrelationId={CorrelationId}",
                queueUrl,
                message.EventId,
                "failed",
                message.CorrelationId);
        }
    }
}
