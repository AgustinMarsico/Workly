namespace CustomExceptions.CompanyExceptions;

public sealed class CompanyAlreadyExistException() : CustomException($"Company already exists.", ErrorStatusCode.Conflict)
{
}
