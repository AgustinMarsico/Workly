namespace CustomExceptions;

public sealed class UserNotFoundException(string email) : CustomException($"User with email '{email}' not found.", ErrorStatusCode.NotFound)
{
}
