namespace CustomExceptions.AreaExceptions;

public sealed class AreaHasReferencesException()
    : CustomException(
        "The area cannot be deleted because it has active references (such as assigned invitations).",
        ErrorStatusCode.Conflict)
{
}
