namespace CustomExceptions;

public sealed class InvalidUserRoleException(string role)
    : CustomException($"Invalid user role '{role}'. Allowed roles are: Admin, Member.", ErrorStatusCode.BadRequest)
{
}
