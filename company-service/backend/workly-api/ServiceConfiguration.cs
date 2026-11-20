using ServiceFactory;
using WebApi.Filters;
using workly_api.Auth;
using workly_api.Observability;

namespace workly_api;
public static class ServiceConfiguration
{
    public static void AddCustomServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ExceptionFilter>();

        services.AddControllers(options =>
        {
            options.Filters.Add<ExceptionFilter>();
        })
        .AddJsonOptions(options =>
        {
            var json = options.JsonSerializerOptions;
            json.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            json.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            json.PropertyNameCaseInsensitive = true;
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.AddServices();
        services.AddTransient<ServiceKeyHttpMessageHandler>();
        services.AddHttpClient<IIdentityApiKeyClient, IdentityApiKeyClient>(client =>
        {
            string? baseUrl = configuration["IdentityService:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            }

            if (int.TryParse(configuration["IdentityService:TimeoutSeconds"], out int timeoutSeconds)
                && timeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            }
        })
        .AddHttpMessageHandler<ServiceKeyHttpMessageHandler>()
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
        services.AddHttpClient<IIdentityAreaUsersClient, IdentityAreaUsersClient>(client =>
        {
            string? baseUrl = configuration["IdentityService:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            }

            if (int.TryParse(configuration["IdentityService:TimeoutSeconds"], out int timeoutSeconds)
                && timeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            }
        })
        .AddHttpMessageHandler<ServiceKeyHttpMessageHandler>()
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
    }
}
