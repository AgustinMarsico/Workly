namespace CustomExceptions.InvitationExceptions;

public sealed class InvitationAlreadyUsedException() : CustomException(
    "La invitación ya ha sido utilizada.",
    ErrorStatusCode.Conflict)
{
}
