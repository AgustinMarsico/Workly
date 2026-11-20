namespace CustomExceptions;

public sealed class InvitationInvalidException(string message)
    : CustomException(message, ErrorStatusCode.BadRequest)
{
}
