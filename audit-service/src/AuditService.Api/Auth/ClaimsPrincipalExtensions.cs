using System.Security.Claims;

namespace AuditService.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid? TryGetCompanyId(this ClaimsPrincipal user)
    {
        string? raw = user.Claims
            .Where(claim => IsCompanyIdClaimType(claim.Type))
            .Select(claim => claim.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return Guid.TryParse(raw, out Guid companyId) && companyId != Guid.Empty
            ? companyId
            : null;
    }

    private static bool IsCompanyIdClaimType(string type)
    {
        return type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase)
            || type.EndsWith("/companyid", StringComparison.OrdinalIgnoreCase);
    }
}
