namespace CustomExceptions.InvitationExceptions;

public sealed class InvitationNotFoundException() : CustomException(
    "La invitación no fue encontrada.",
    ErrorStatusCode.NotFound)
{
}
