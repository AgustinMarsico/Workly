namespace CustomExceptions;

public sealed class CreateUserException(string message)
    : CustomException(message, ErrorStatusCode.BadRequest)
{
}
