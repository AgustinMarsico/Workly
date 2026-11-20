namespace CustomExceptions;

public sealed class UnauthorizedException(string message)
    : CustomException(message, ErrorStatusCode.Unauthorized)
{
}
