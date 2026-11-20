using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using WorklyReservationService.Domain;
using WorklyReservationService.IDataAccess.External;

namespace WorklyReservationService.WebApi.Auth;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyClient _apiKeys;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyClient apiKeys)
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

        ApiKeyValidationResponse? validation;
        try
        {
            validation = await _apiKeys.ValidateAsync(apiKeyValue, Context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (NotSupportedException ex)
        {
            Logger.LogWarning(ex, "API key validation endpoint is not available in the main service.");
            return AuthenticateResult.Fail(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "API key validation is not configured.");
            return AuthenticateResult.Fail(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "API key validation request failed.");
            return AuthenticateResult.Fail("API key validation failed.");
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogWarning(ex, "API key validation request timed out.");
            return AuthenticateResult.Fail("API key validation failed.");
        }

        if (validation is null || !validation.IsValid || !validation.IsActive)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (!string.Equals(validation.Role, UserRoles.Member, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("API key user is not a Member.");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, validation.UserId.ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Member),
        };

        if (validation.CompanyId is Guid companyId)
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
