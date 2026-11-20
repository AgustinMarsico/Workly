using System.Net.Http.Headers;

namespace workly_api.Reservations;

public sealed record ReservationServiceAuthContext(string HeaderName, string Value)
{
    public static ReservationServiceAuthContext Bearer(string token) => new("Authorization", $"Bearer {token}");

    public static ReservationServiceAuthContext ApiKey(string apiKey) => new("API_KEY", apiKey);

    public void Apply(HttpRequestMessage request)
    {
        if (string.Equals(HeaderName, "Authorization", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(Value);
            return;
        }

        request.Headers.TryAddWithoutValidation(HeaderName, Value);
    }
}
