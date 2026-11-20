using System.Text;
using CustomExceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using workly_api;

namespace workly_api.Auth;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddWorklyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSecret = configuration["SUPER_SECRET_KEY"]
            ?? throw new InvalidOperationException(
                "JWT secret not configured. Set SUPER_SECRET_KEY in appsettings or environment.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "WorklyAuth";
                options.DefaultChallengeScheme = "WorklyAuth";
            })
            .AddPolicyScheme("WorklyAuth", "WorklyAuth", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    // Prefer JWT if both are present.
                    string authorization = context.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Bearer";
                    }

                    if (context.Request.Headers.ContainsKey("API_KEY")
                        || context.Request.Headers.ContainsKey("X-API-KEY"))
                    {
                        return ApiKeyAuthenticationOptions.SchemeName;
                    }

                    return "Bearer";
                };
            })
            .AddJwtBearer("Bearer", options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        if (context.Response.HasStarted)
                        {
                            return Task.CompletedTask;
                        }

                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsJsonAsync(
                            new ApiErrorResponse(StatusCodes.Status401Unauthorized, "Not authorized. Please provide a valid token."));
                    },
                    OnForbidden = context =>
                    {
                        if (context.Response.HasStarted)
                        {
                            return Task.CompletedTask;
                        }

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsJsonAsync(
                            new ApiErrorResponse(StatusCodes.Status403Forbidden, "Forbidden. You do not have permission to access this resource."));
                    }
                };
            })
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationOptions.SchemeName,
                options =>
                {
                    options.HeaderName = "API_KEY";
                });

        services.AddAuthorization();

        return services;
    }
}
