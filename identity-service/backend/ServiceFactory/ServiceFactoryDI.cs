using BusinessLogic;
using DataAccess;
using IBusinessLogic;
using IBusinessLogic.Organization;
using IDataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceFactory;

public static class ServiceFactoryDI
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IApiKeyService, ApiKeyService>();

        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
    }
}
