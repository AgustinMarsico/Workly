using AuditService.Api.Domain;

namespace AuditService.Tests;

[TestClass]
public sealed class AuditActionTypesTests
{
    [TestMethod]
    public void Normalize_ThrowsArgumentException_WhenActionTypeIsUnsupported()
    {
        ArgumentException ex = Assert.ThrowsException<ArgumentException>(
            () => AuditActionTypes.Normalize("UnsupportedAction"));

        Assert.AreEqual("Unsupported audit action type: UnsupportedAction (Parameter 'actionType')", ex.Message);
    }
}
