using System.Security.Claims;

namespace WorklyReservationService.WebApi.Audit;

public static class ClaimsPrincipalAuditExtensions
{
    public static string? GetAuditUserEmail(this ClaimsPrincipal user)
    {
        string? email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("email")
            ?? user.FindFirstValue("Email");

        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }
}
