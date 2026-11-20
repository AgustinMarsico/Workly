using System.Security.Claims;
using AuditService.Api.Auth;

namespace AuditService.Tests;

[TestClass]
public sealed class ClaimsPrincipalExtensionsTests
{
    [TestMethod]
    public void TryGetCompanyId_ReturnsCompanyId_WhenClaimTypeEndsWithCompanyId()
    {
        Guid companyId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("https://workly.test/companyid", companyId.ToString())],
            authenticationType: "Test"));

        Guid? result = principal.TryGetCompanyId();

        Assert.IsNotNull(result);
        Assert.AreEqual(companyId, result!.Value);
    }
}
