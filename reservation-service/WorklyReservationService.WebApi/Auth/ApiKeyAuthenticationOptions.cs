using Microsoft.AspNetCore.Authentication;

namespace WorklyReservationService.WebApi.Auth;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ApiKey";

    public string HeaderName { get; set; } = "API_KEY";
}
