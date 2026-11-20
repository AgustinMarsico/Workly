using CustomExceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessTest;

[TestClass]
public sealed class CustomExceptionTests
{
    [TestMethod]
    public void Constructor_TrimsMessage_AndStoresStatusCode()
    {
        var exception = new TestCustomException("  Invalid request.  ", ErrorStatusCode.BadRequest);

        Assert.AreEqual("Invalid request.", exception.Message);
        Assert.AreEqual(ErrorStatusCode.BadRequest, exception.StatusCode);
    }

    [TestMethod]
    public void Constructor_ThrowsArgumentException_WhenMessageIsEmpty()
    {
        try
        {
            _ = new TestCustomException("   ", ErrorStatusCode.BadRequest);
            Assert.Fail("Expected an ArgumentException to be thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("Exception message cannot be null or empty.", ex.Message);
        }
    }

    private sealed class TestCustomException(string message, ErrorStatusCode statusCode)
        : CustomException(message, statusCode);
}
