using System.Net.Http.Headers;

namespace WorklyReservationService.IDataAccess.External;

public sealed record MainServiceAuthContext(string HeaderName, string Value)
{
    public static MainServiceAuthContext Bearer(string token) => new("Authorization", $"Bearer {token}");

    public static MainServiceAuthContext ApiKey(string apiKey) => new("API_KEY", apiKey);

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
