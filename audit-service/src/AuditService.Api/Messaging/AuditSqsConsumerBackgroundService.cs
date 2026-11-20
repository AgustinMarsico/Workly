using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using AuditService.Api.Contracts;
using AuditService.Api.Services;
using Serilog.Context;

namespace AuditService.Api.Messaging;

public sealed class AuditSqsConsumerBackgroundService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAmazonSQS sqs;
    private readonly IConfiguration configuration;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<AuditSqsConsumerBackgroundService> logger;

    public AuditSqsConsumerBackgroundService(
        IAmazonSQS sqs,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<AuditSqsConsumerBackgroundService> logger)
    {
        this.sqs = sqs;
        this.configuration = configuration;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string? queueUrl = configuration["SQS_AUDIT_QUEUE_URL"];
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            logger.LogWarning("SQS_AUDIT_QUEUE_URL is not configured; audit consumer is stopped.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20,
                    MessageAttributeNames = ["All"]
                }, stoppingToken);

                if (response.Messages is null || response.Messages.Count == 0)
                {
                    await LogQueueDepth(queueUrl, stoppingToken);
                    continue;
                }

                logger.LogInformation(
                    "SQS audit messages received: Queue={QueueUrl} Count={MessageCount} Status={Status}",
                    queueUrl,
                    response.Messages.Count,
                    "pending");

                foreach (Message message in response.Messages)
                {
                    await ProcessMessage(queueUrl, message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error receiving audit events from SQS.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessMessage(string queueUrl, Message message, CancellationToken cancellationToken)
    {
        string? correlationId = GetCorrelationId(message);
        using var correlationScope = LogContext.PushProperty("CorrelationId", correlationId);

        try
        {
            AuditEvent? auditEvent = JsonSerializer.Deserialize<AuditEvent>(message.Body, JsonOptions);
            if (auditEvent is null)
            {
                logger.LogWarning(
                    "SQS audit message invalid: Queue={QueueUrl} MessageId={MessageId} Status={Status} CorrelationId={CorrelationId}",
                    queueUrl,
                    message.MessageId,
                    "failed",
                    correlationId);
                await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
                return;
            }

            using IServiceScope scope = serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IAuditEventService>();
            await service.ValidateAndSaveAsync(auditEvent, cancellationToken);
            await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
            logger.LogInformation(
                "SQS audit message processed: Queue={QueueUrl} MessageId={MessageId} EventId={EventId} Status={Status} CorrelationId={CorrelationId}",
                queueUrl,
                message.MessageId,
                auditEvent.EventId,
                "processed",
                auditEvent.CorrelationId ?? correlationId);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "SQS audit message invalid JSON: Queue={QueueUrl} MessageId={MessageId} Status={Status} CorrelationId={CorrelationId}",
                queueUrl,
                message.MessageId,
                "failed",
                correlationId);
            await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(
                ex,
                "SQS audit message invalid payload: Queue={QueueUrl} MessageId={MessageId} Status={Status} CorrelationId={CorrelationId}",
                queueUrl,
                message.MessageId,
                "failed",
                correlationId);
            await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "SQS audit message processing failed: Queue={QueueUrl} MessageId={MessageId} Status={Status} CorrelationId={CorrelationId}",
                queueUrl,
                message.MessageId,
                "failed",
                correlationId);
        }
    }

    private async Task LogQueueDepth(string queueUrl, CancellationToken cancellationToken)
    {
        try
        {
            var attributes = await sqs.GetQueueAttributesAsync(
                queueUrl,
                ["ApproximateNumberOfMessages", "ApproximateNumberOfMessagesNotVisible"],
                cancellationToken);

            attributes.Attributes.TryGetValue("ApproximateNumberOfMessages", out string? pending);
            attributes.Attributes.TryGetValue("ApproximateNumberOfMessagesNotVisible", out string? inFlight);

            logger.LogInformation(
                "SQS audit queue depth: Queue={QueueUrl} PendingMessages={PendingMessages} InFlightMessages={InFlightMessages}",
                queueUrl,
                pending ?? "0",
                inFlight ?? "0");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read SQS audit queue depth: Queue={QueueUrl}", queueUrl);
        }
    }

    private static string? GetCorrelationId(Message message)
    {
        if (message.MessageAttributes is not null
            && message.MessageAttributes.TryGetValue("X-Correlation-ID", out var attribute))
        {
            return attribute.StringValue;
        }

        return null;
    }
}
