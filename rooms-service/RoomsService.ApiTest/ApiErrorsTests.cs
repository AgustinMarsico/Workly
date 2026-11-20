using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using workly_api;

namespace RoomsService.ApiTest;

[TestClass]
public sealed class ApiErrorsTests
{
    [TestMethod]
    public void Unauthorized_ReturnsUnauthorizedErrorResponse()
    {
        ObjectResult result = ApiErrors.Unauthorized("Not authorized.");

        AssertApiError(result, StatusCodes.Status401Unauthorized, "Not authorized.");
    }

    [TestMethod]
    public void Conflict_ReturnsConflictErrorResponse()
    {
        ObjectResult result = ApiErrors.Conflict("Conflict.");

        AssertApiError(result, StatusCodes.Status409Conflict, "Conflict.");
    }

    [TestMethod]
    public void InternalServerError_ReturnsInternalServerErrorResponse()
    {
        ObjectResult result = ApiErrors.InternalServerError("Unexpected error.");

        AssertApiError(result, StatusCodes.Status500InternalServerError, "Unexpected error.");
    }

    private static void AssertApiError(ObjectResult result, int expectedStatusCode, string expectedMessage)
    {
        Assert.AreEqual(expectedStatusCode, result.StatusCode);
        var response = result.Value as ApiErrorResponse;
        Assert.IsNotNull(response);
        Assert.AreEqual(expectedStatusCode, response.StatusCode);
        Assert.AreEqual(expectedMessage, response.Message);
    }
}
