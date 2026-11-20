namespace WorklyReservationService.WebApi.Auth;

public sealed class ServiceKeyHttpMessageHandler(IConfiguration configuration) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? serviceKey = configuration["INTERNAL_SERVICE_KEY"];
        if (!string.IsNullOrWhiteSpace(serviceKey))
        {
            request.Headers.Remove(ServiceKeyAuthenticationOptions.DefaultHeaderName);
            request.Headers.TryAddWithoutValidation(
                ServiceKeyAuthenticationOptions.DefaultHeaderName,
                serviceKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
