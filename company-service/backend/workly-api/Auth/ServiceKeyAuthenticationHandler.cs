using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace workly_api.Auth;

public sealed class ServiceKeyAuthenticationHandler(
    IOptionsMonitor<ServiceKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<ServiceKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string provided = values.ToString().Trim();
        string? expected = configuration["INTERNAL_SERVICE_KEY"];

        if (string.IsNullOrWhiteSpace(expected))
        {
            return Task.FromResult(AuthenticateResult.Fail("Internal service key is not configured."));
        }

        if (!FixedTimeEquals(provided, expected))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid service key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationRoleNames.InternalService),
        };
        var identity = new ClaimsIdentity(claims, ServiceKeyAuthenticationOptions.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ServiceKeyAuthenticationOptions.SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        ReadOnlySpan<byte> leftBytes = Encoding.UTF8.GetBytes(left);
        ReadOnlySpan<byte> rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
