using Microsoft.AspNetCore.Authentication;

namespace workly_api.Auth;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ApiKey";
    public const string DefaultHeaderName = "API_KEY";

    public string HeaderName { get; set; } = DefaultHeaderName;
}
