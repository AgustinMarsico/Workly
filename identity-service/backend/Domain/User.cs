
using CustomExceptions;

namespace Domain;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public Guid? AreaId { get; set; } = null;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
            throw new CreateUserException("Email cannot be empty.");

        if (string.IsNullOrWhiteSpace(PasswordHash))
            throw new CreateUserException("Password cannot be empty.");

        if (!UserRoles.TryNormalize(Role, out var normalizedRole))
            throw new InvalidUserRoleException(Role);

        Email = Email.Trim();
        Role = normalizedRole;

        if (Role == UserRoles.Admin)
        {
            if (CompanyId is not null)
                throw new CreateUserException("CompanyId must not be provided for Admin users.");
        }
        else if (Role == UserRoles.Member)
        {
            if (CompanyId is null)
                throw new CreateUserException("CompanyId is required for Member users.");

            if (CompanyId.Value == Guid.Empty)
                throw new CreateUserException("CompanyId cannot be empty.");
            
            if (AreaId is not null && AreaId.Value == Guid.Empty)
                throw new CreateUserException("AreaId cannot be empty if provided.");
        }
    }
}