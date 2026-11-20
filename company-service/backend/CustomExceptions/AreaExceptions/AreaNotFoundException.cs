namespace CustomExceptions.AreaExceptions;

public sealed class AreaNotFoundException() : CustomException("The area was not found.", ErrorStatusCode.NotFound)
{
}