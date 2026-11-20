namespace CustomExceptions;

public sealed class UserAlreadyExistsException(string email)
    : CustomException($"User with email '{email}' already exists.", ErrorStatusCode.Conflict)
{
}
