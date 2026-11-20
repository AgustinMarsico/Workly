using Amazon.SQS;
using AuditService.Api.Auth;
using AuditService.Api.Data;
using AuditService.Api.Data.Seed;
using AuditService.Api.Middleware;
using AuditService.Api.Messaging;
using AuditService.Api.Services;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text.Json.Serialization;

LoadEnvironment();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .Enrich.WithProperty("service.name", "audit-service")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    bool runMigrations = args.Contains("migrate", StringComparer.OrdinalIgnoreCase);

    builder.Host.UseSerilog();

    string connectionString = ResolveConnectionString(builder.Configuration, runMigrations);
    builder.Services.AddDbContext<AuditDbContext>(options =>
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

    builder.Services.AddScoped<IAuditRecordRepository, EfAuditRecordRepository>();
    builder.Services.AddScoped<IAuditEventService, AuditEventService>();
    builder.Services.AddAWSService<IAmazonSQS>();

    if (!builder.Configuration.GetValue("SKIP_AUDIT_QUEUE", false))
    {
        builder.Services.AddHostedService<AuditSqsConsumerBackgroundService>();
    }

    builder.Services.AddAuditAuthentication(builder.Configuration);
    builder.Services.AddAuditAuthorization();

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

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    if (runMigrations)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await db.Database.EnsureCreatedAsync();
        await AuditDbInitializer.SeedAsync(db);
        await AuditDatabasePermissions.ApplyRestrictedUserPermissions(db, app.Configuration);
        return;
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("DynamicCors");

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<RequestResponseLoggingMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Audit Service failed to start");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

static void LoadEnvironment()
{
    string[] candidates =
    [
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env")
    ];

    foreach (string candidate in candidates)
    {
        if (File.Exists(candidate))
        {
            Env.Load(candidate);
            return;
        }
    }
}

static string ResolveConnectionString(IConfiguration configuration, bool migrationMode)
{
    string? connectionString = migrationMode
        ? configuration.GetConnectionString("AuditMigrationDatabase")
        : configuration.GetConnectionString("AuditDatabase");

    connectionString ??= configuration.GetConnectionString("AuditDatabase");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings__AuditDatabase is not configured.");
    }

    return connectionString;
}
