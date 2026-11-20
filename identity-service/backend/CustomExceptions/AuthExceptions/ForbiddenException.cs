namespace CustomExceptions;

public sealed class ForbiddenException(string message)
    : CustomException(message, ErrorStatusCode.Forbidden)
{
}
