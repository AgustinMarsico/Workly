using Microsoft.AspNetCore.Authentication;

namespace workly_api.Auth;

public sealed class ServiceKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ServiceKey";
    public const string DefaultHeaderName = "X-Service-Key";

    public string HeaderName { get; set; } = DefaultHeaderName;
}
