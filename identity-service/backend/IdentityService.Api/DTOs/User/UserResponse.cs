namespace workly_api.DTOs.User;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string Role,
    Guid? CompanyId,
    Guid? AreaId,
    bool EmailSubscribed = true
);

public static class UserResponseMappings
{
    public static UserResponse ToResponse(this Domain.User user, bool emailSubscribed = true) =>
        new(user.Id, user.Email, user.Role, user.CompanyId, user.AreaId, emailSubscribed);
}
