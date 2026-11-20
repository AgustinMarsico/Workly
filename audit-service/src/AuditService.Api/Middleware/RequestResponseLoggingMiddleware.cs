using System.Diagnostics;
using Serilog.Context;

namespace AuditService.Api.Middleware;

public sealed class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<RequestResponseLoggingMiddleware> logger;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var correlationId = context.Items["CorrelationId"] as string;

        try
        {
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next(context);

                stopwatch.Stop();
                var logLevel = context.Response.StatusCode >= StatusCodes.Status500InternalServerError
                    ? LogLevel.Error
                    : LogLevel.Information;

                logger.Log(
                    logLevel,
                    "HTTP {Method} {Path} responded {StatusCode} in {DurationMs} ms",
                    requestMethod,
                    requestPath,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "Unhandled exception in middleware for {Method} {Path} after {DurationMs} ms",
                requestMethod,
                requestPath,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
