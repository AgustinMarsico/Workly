namespace IBusinessLogic;
public interface IAuthService
{
    Task<string> Login(string email, string password);
    Task<Domain.User> Register(string email, string password, string role, Guid? companyId, Guid? areaId);
}
