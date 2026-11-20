namespace WorklyReservationService.IDataAccess.External;

public interface IApiKeyClient
{
    Task<ApiKeyValidationResponse?> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default);
}
