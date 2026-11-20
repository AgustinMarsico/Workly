namespace CustomExceptions.CompanyExceptions;
public sealed class CreateCompanyException(string message) : CustomException(message, ErrorStatusCode.BadRequest)
{
}