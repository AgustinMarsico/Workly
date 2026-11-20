using CustomExceptions;
using IDataAccess;
using Domain;
using IBusinessLogic;
using IBusinessLogic.Organization;

namespace BusinessLogic;

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly IJwtService _jwt;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IOrganizationReferenceClient _organizationReferences;

    public AuthService(
        IUserRepository repo,
        IJwtService jwt,
        IPasswordHasherService passwordHasher,
        IOrganizationReferenceClient organizationReferences)
    {
        _repo = repo;
        _jwt = jwt;
        _passwordHasher = passwordHasher;
        _organizationReferences = organizationReferences;
    }

    public async Task<string> Login(string email, string password)
    {
        var user = await _repo.GetByEmail(email);

        if (user == null || !_passwordHasher.VerifyPassword(password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials.");

        return _jwt.GenerateToken(user);
    }

    public async Task<User> Register(string email, string password, string role, Guid? companyId, Guid? areaId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new CreateUserException("Email cannot be empty.");

        if (string.IsNullOrWhiteSpace(password))
            throw new CreateUserException("Password cannot be empty.");

        if (string.IsNullOrWhiteSpace(role))
            throw new CreateUserException("Role cannot be empty.");

        if (!UserRoles.TryNormalize(role, out var normalizedRole))
            throw new InvalidUserRoleException(role);

        if (normalizedRole == UserRoles.Admin)
        {
            if (companyId is not null)
                throw new CreateUserException("CompanyId must not be provided for Admin users.");
        }
        else if (normalizedRole == UserRoles.Member)
        {
            if (companyId is null)
                throw new CreateUserException("CompanyId is required for Member users.");

            if (companyId.Value == Guid.Empty)
                throw new CreateUserException("CompanyId cannot be empty.");
            
            if (areaId is not null && areaId.Value == Guid.Empty)
                throw new CreateUserException("AreaId cannot be empty if provided.");

            if (areaId is not null)
                await _organizationReferences.GetCompanyAreaAsync(companyId.Value, areaId.Value);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.HashPassword(password),
            Role = normalizedRole,
            CompanyId = companyId,
            AreaId = areaId
        };

        user.Validate();

        return await _repo.CreateAsync(user);
    }
}
