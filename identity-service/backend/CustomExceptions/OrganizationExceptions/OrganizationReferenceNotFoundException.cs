using CustomExceptions;

namespace CustomExceptions.OrganizationExceptions;

public sealed class OrganizationReferenceNotFoundException : CustomException
{
    public OrganizationReferenceNotFoundException(string message)
        : base(message, ErrorStatusCode.NotFound)
    {
    }
}
