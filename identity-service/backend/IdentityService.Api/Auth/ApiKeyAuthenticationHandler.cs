using System.Security.Claims;
using System.Text.Encodings.Web;
using CustomExceptions;
using Domain;
using IBusinessLogic;
using IBusinessLogic.ApiKeys;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using workly_api;

namespace workly_api.Auth;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyService _apiKeys;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyService apiKeys)
        : base(options, logger, encoder)
    {
        _apiKeys = apiKeys;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var values)
            && !Request.Headers.TryGetValue("X-API-KEY", out values))
        {
            return AuthenticateResult.NoResult();
        }

        string apiKeyValue = values.ToString().Trim();
        if (string.IsNullOrWhiteSpace(apiKeyValue))
        {
            return AuthenticateResult.Fail("Missing API key.");
        }

        if (!IsMemberEndpoint())
        {
            return AuthenticateResult.Fail("API keys can only access Member endpoints.");
        }

        ApiKeyValidationResult apiKey = await _apiKeys.ValidateAsync(apiKeyValue, Context.RequestAborted)
            .ConfigureAwait(false);

        if (!apiKey.IsValid || !apiKey.IsActive)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (!string.Equals(apiKey.Role, UserRoles.Member, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("API key user is not a Member.");
        }

        if (apiKey.UserId is not Guid userId)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Member),
        };

        if (apiKey.CompanyId is Guid companyId)
        {
            claims.Add(new Claim("CompanyId", companyId.ToString()));
        }

        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationOptions.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationOptions.SchemeName);
        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        return Response.WriteAsJsonAsync(
            new ApiErrorResponse(StatusCodes.Status401Unauthorized, "Not authorized. Please provide a valid token or API key."));
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        if (Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json";
        return Response.WriteAsJsonAsync(
            new ApiErrorResponse(StatusCodes.Status403Forbidden, "Forbidden. You do not have permission to access this resource."));
    }

    private bool IsMemberEndpoint()
    {
        var endpoint = Context.GetEndpoint();
        if (endpoint is null)
        {
            return false;
        }

        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        if (authorizeData is null || authorizeData.Count == 0)
        {
            return false;
        }

        foreach (IAuthorizeData data in authorizeData)
        {
            if (string.IsNullOrWhiteSpace(data.Roles))
            {
                continue;
            }

            string[] roles = data.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (roles.Any(r => string.Equals(r, UserRoles.Member, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
