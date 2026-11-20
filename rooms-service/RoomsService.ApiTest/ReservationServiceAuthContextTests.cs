using Microsoft.VisualStudio.TestTools.UnitTesting;
using workly_api.Reservations;

namespace RoomsService.ApiTest;

[TestClass]
public sealed class ReservationServiceAuthContextTests
{
    [TestMethod]
    public void Apply_SetsAuthorizationHeader_WhenBearer()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://workly.test/api/reservations/top-rooms");
        ReservationServiceAuthContext authentication = ReservationServiceAuthContext.Bearer("token-123");

        authentication.Apply(request);

        Assert.IsNotNull(request.Headers.Authorization);
        Assert.AreEqual("Bearer", request.Headers.Authorization!.Scheme);
        Assert.AreEqual("token-123", request.Headers.Authorization.Parameter);
    }

    [TestMethod]
    public void Apply_AddsApiKeyHeader_WhenApiKey()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://workly.test/api/reservations/top-rooms");
        ReservationServiceAuthContext authentication = ReservationServiceAuthContext.ApiKey("api-key-123");

        authentication.Apply(request);

        Assert.IsTrue(request.Headers.TryGetValues("API_KEY", out IEnumerable<string>? values));
        Assert.AreEqual("api-key-123", values!.Single());
    }
}
