using System.Net;
using System.Net.Http.Json;
using Domain;

namespace workly_api.Reservations;

public sealed class MainReservationServiceClient : IReservationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public MainReservationServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<TopRoomDto>> GetTopRoomsAsync(
        ReservationServiceAuthContext authentication,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        string path = _configuration["ReservationService:TopRoomsPath"]
            ?? "/api/reservations/top-rooms";

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        authentication.Apply(request);

        using HttpResponseMessage response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException("Reservation service rejected the forwarded credentials.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<TopRoomDto>>(cancellationToken)
            .ConfigureAwait(false) ?? [];
    }

    private void EnsureConfigured()
    {
        if (_httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException("ReservationService:BaseUrl is not configured.");
        }
    }
}
