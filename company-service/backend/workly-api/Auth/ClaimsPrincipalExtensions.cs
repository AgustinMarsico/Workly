using System.Linq;
using System.Security.Claims;

namespace workly_api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
            throw new UnauthorizedAccessException("User id claim not found.");

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User id claim is not a valid Guid.");

        return userId;
    }

    public static Guid GetCompanyId(this ClaimsPrincipal user)
    {
        string? raw = user.Claims
            .Where(static c => IsCompanyIdClaimType(c.Type))
            .Select(static c => c.Value)
            .FirstOrDefault(static v => !string.IsNullOrWhiteSpace(v));

        if (string.IsNullOrWhiteSpace(raw)
            || !Guid.TryParse(raw, out var companyId)
            || companyId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("CompanyId claim not found.");
        }

        return companyId;
    }

    private static bool IsCompanyIdClaimType(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return false;
        }

        if (string.Equals(type, "CompanyId", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return type.EndsWith("/companyid", StringComparison.OrdinalIgnoreCase);
    }
}