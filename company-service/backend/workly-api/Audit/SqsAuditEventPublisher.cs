using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace workly_api.Audit;

public sealed class SqsAuditEventPublisher : IAuditEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAmazonSQS sqs;
    private readonly IConfiguration configuration;
    private readonly ILogger<SqsAuditEventPublisher> logger;

    public SqsAuditEventPublisher(
        IAmazonSQS sqs,
        IConfiguration configuration,
        ILogger<SqsAuditEventPublisher> logger)
    {
        this.sqs = sqs;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task PublishAsync(AuditEventMessage message, CancellationToken cancellationToken = default)
    {
        string? queueUrl = configuration["SQS_AUDIT_QUEUE_URL"];
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

            var response = await sqs.SendMessageAsync(request, cancellationToken);
            logger.LogInformation(
                "SQS audit event published: Queue={QueueUrl} EventId={EventId} MessageId={MessageId} Status={Status} CorrelationId={CorrelationId}",
                queueUrl,
                message.EventId,
                response.MessageId,
                "processed",
                message.CorrelationId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "SQS audit event publish failed: Queue={QueueUrl} EventId={EventId} Status={Status} CorrelationId={CorrelationId}",
                queueUrl,
                message.EventId,
                "failed",
                message.CorrelationId);
        }
    }
}
