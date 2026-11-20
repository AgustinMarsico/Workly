using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AuditService.Api.Auth;

public static class AuditAuthenticationExtensions
{
    public static IServiceCollection AddAuditAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string jwtSecret = configuration["SUPER_SECRET_KEY"]
            ?? throw new InvalidOperationException("SUPER_SECRET_KEY is required to validate Workly JWT tokens.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        return services;
    }

    public static IServiceCollection AddAuditAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuditAuthorizationPolicies.AdminOnly, policy =>
                policy.RequireAuthenticatedUser().RequireRole(UserRoles.Admin));
        });

        return services;
    }
}
