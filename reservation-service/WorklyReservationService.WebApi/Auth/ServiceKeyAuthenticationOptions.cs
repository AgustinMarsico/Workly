using Microsoft.AspNetCore.Authentication;

namespace WorklyReservationService.WebApi.Auth;

public sealed class ServiceKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ServiceKey";
    public const string DefaultHeaderName = "X-Service-Key";

    public string HeaderName { get; set; } = DefaultHeaderName;
}
