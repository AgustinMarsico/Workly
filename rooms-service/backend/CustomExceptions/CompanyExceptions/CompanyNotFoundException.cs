namespace CustomExceptions.CompanyExceptions;

public sealed class CompanyNotFoundException() : CustomException("The company was not found.", ErrorStatusCode.NotFound)
{
}
