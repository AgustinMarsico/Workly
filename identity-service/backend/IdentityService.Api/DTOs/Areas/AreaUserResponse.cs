using IDataAccess;

namespace workly_api.DTOs.Areas;

public sealed record AreaUserResponse(
    Guid Id,
    string Email,
    string? Name,
    Guid? AreaId,
    bool EmailSubscribed);

public static class AreaUserResponseMappings
{
    public static AreaUserResponse ToResponse(this AreaUser user) =>
        new(
            user.Id,
            user.Email,
            ResolveName(user.Email),
            user.AreaId,
            user.EmailSubscribed);

    private static string? ResolveName(string email)
    {
        int atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return null;
        }

        return email[..atIndex];
    }
}
