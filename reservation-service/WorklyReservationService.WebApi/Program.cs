using System.Text.Json;
using DotNetEnv;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;
using WorklyReservationService.DataAccess;
using WorklyReservationService.WebApi;
using WorklyReservationService.WebApi.Audit;
using WorklyReservationService.WebApi.Auth;
using WorklyReservationService.WebApi.Middleware;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    Env.NoClobber().Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service.name", "reservation-service")
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<WorklyReservationService.WebApi.Observability.CorrelationIdDelegatingHandler>();

builder.Services.AddWorklyReservationPersistence(builder.Configuration);
builder.Services.AddCustomServices(builder.Configuration);
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
builder.Services.AddAWSService<IAmazonSQS>();
string? skipAuditQueue = builder.Configuration["SKIP_AUDIT_QUEUE"];
if (skipAuditQueue?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
{
    builder.Services.AddScoped<IAuditEventPublisher, NoOpAuditEventPublisher>();
}
else
{
    builder.Services.AddScoped<IAuditEventPublisher, SqsAuditEventPublisher>();
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme.",
        });

    options.AddSecurityDefinition(
        ApiKeyAuthenticationOptions.SchemeName,
        new OpenApiSecurityScheme
        {
            Name = "API_KEY",
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "API key emitida para usuarios Member.",
        });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = ApiKeyAuthenticationOptions.SchemeName,
                },
            },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DynamicCors", policy =>
    {
        policy
            .SetIsOriginAllowed(origin => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddWorklyReservationAuthentication(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();

        if (feature?.Error != null)
        {
            var logger = context.RequestServices.GetService<ILogger<Program>>();
            logger?.LogError(
                feature.Error,
                "Unhandled exception in endpoint: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }

        if (feature?.Error is JsonException or BadHttpRequestException)
        {
            string message = feature.Error is JsonException jsonException
                && !string.IsNullOrWhiteSpace(jsonException.Message)
                ? jsonException.Message
                : "Invalid request body.";

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse(StatusCodes.Status400BadRequest, message));
            return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(
            new ApiErrorResponse(StatusCodes.Status500InternalServerError, "There was an error when processing the request"));
    });
});

if (args.Contains("migrate"))
{
    await MigrationRunner.Run(app.Services);
    return;
}

await MigrationRunner.Run(app.Services);

app.UseCors("DynamicCors");

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    if (response.HasStarted)
    {
        return;
    }

    string message = response.StatusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad request.",
        StatusCodes.Status401Unauthorized => "Not authorized.",
        StatusCodes.Status403Forbidden => "Forbidden.",
        StatusCodes.Status404NotFound => "The requested resource was not found.",
        StatusCodes.Status405MethodNotAllowed => "Method not allowed.",
        _ => "There was an error when processing the request",
    };

    response.ContentType = "application/json";
    await response.WriteAsJsonAsync(new ApiErrorResponse(response.StatusCode, message));
});

app.MapControllers();

app.Run();
