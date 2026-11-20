using IBusinessLogic;

namespace workly_api.DTOs.User;

public record CreateUserRequest(
    string Email,
    string Password,
    string Role,
    Guid? CompanyId
)
{
    public Domain.User ToEntity(IPasswordHasherService passwordHasher)
    {
        return new Domain.User
        {
            Email = Email,
            PasswordHash = passwordHasher.HashPassword(Password),
            Role = Role,
            CompanyId = CompanyId
        };
    }
}
