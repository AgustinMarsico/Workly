using WorklyReservationService.BusinessLogic;
using WorklyReservationService.DataAccess;
using WorklyReservationService.DataAccess.External;
using WorklyReservationService.IBusinessLogic;
using WorklyReservationService.IBusinessLogic.Notifications;
using WorklyReservationService.IDataAccess;
using WorklyReservationService.IDataAccess.External;
using WorklyReservationService.WebApi.Auth;
using WorklyReservationService.WebApi.Filters;
using WorklyReservationService.WebApi.Notifications;
using WorklyReservationService.WebApi.Observability;

namespace WorklyReservationService.WebApi;

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

        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IAreaNotificationRecipientRepository, AreaNotificationRecipientRepository>();
        services.AddScoped<ReservationNotificationService>();
        services.AddRoomsServiceHttpClient<IRoomClient, MainServiceRoomClient>(configuration);
        services.AddMainServiceHttpClient<IAreaUsersClient, MainServiceAreaUsersClient>(configuration);
        services.AddIdentityServiceHttpClient<IUserClient, MainServiceUserClient>(configuration);
        services.AddIdentityServiceHttpClient<IApiKeyClient, MainServiceApiKeyClient>(configuration);

        string? skipReservationNotification = configuration["SKIP_RESERVATION_NOTIFICATION"];
        if (skipReservationNotification?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddScoped<IReservationNotificationPublisher, NoOpReservationNotificationPublisher>();
        }
        else
        {
            services.AddScoped<IReservationNotificationPublisher, SnsReservationNotificationPublisher>();
        }
    }

    private static void AddMainServiceHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>(client =>
        {
            string? baseUrl = configuration["MainService:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            }

            if (int.TryParse(configuration["MainService:TimeoutSeconds"], out int timeoutSeconds)
                && timeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            }
        })
        .AddHttpMessageHandler<ServiceKeyHttpMessageHandler>()
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
    }

    private static void AddRoomsServiceHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>(client =>
        {
            string? baseUrl = configuration["RoomsService:BaseUrl"]
                ?? configuration["MainService:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            }

            if (int.TryParse(configuration["RoomsService:TimeoutSeconds"], out int timeoutSeconds)
                && timeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            }
            else if (int.TryParse(configuration["MainService:TimeoutSeconds"], out int mainTimeoutSeconds)
                && mainTimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(mainTimeoutSeconds);
            }
        })
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
    }

    private static void AddIdentityServiceHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>(client =>
        {
            string? baseUrl = configuration["IdentityService:BaseUrl"]
                ?? configuration["MainService:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            }

            if (int.TryParse(configuration["IdentityService:TimeoutSeconds"], out int timeoutSeconds)
                && timeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            }
            else if (int.TryParse(configuration["MainService:TimeoutSeconds"], out int mainTimeoutSeconds)
                && mainTimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(mainTimeoutSeconds);
            }
        })
        .AddHttpMessageHandler<ServiceKeyHttpMessageHandler>()
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
    }
}
