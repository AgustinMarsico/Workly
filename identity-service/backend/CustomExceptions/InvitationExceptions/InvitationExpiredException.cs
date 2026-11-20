namespace CustomExceptions.InvitationExceptions;

public sealed class InvitationExpiredException() : CustomException(
    "La invitación ha expirado.",
    ErrorStatusCode.BadRequest)
{
}
